using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MarkTogether.Shared;

namespace MarkTogether.Client.Network
{
    /// <summary>
    /// Singleton TCP client với cơ chế correlation:
    /// - Thread đọc nền `ReceiveLoop()` đọc liên tục.
    /// - Mỗi request có RequestId; khi response về, dispatcher unblock đúng task.
    /// - Packet không có RequestId được coi là server-push → raise event tương ứng.
    /// </summary>
    public class SocketClient
    {
        private TcpClient _tcp;
        private Stream _stream;
        private bool _connected;
        private Thread _receiveThread;
        private volatile bool _stopReceive;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<Packet>> _pending
            = new ConcurrentDictionary<string, TaskCompletionSource<Packet>>();

        public static SocketClient Instance { get; } = new SocketClient();

        public string Token { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        // ─── Server-push events ───
        public event Action<Payload_OP_BROADCAST> OnOpBroadcast;
        public event Action<Payload_CHAT_BROADCAST> OnChatBroadcast;
        public event Action<Payload_COMMENT_BROADCAST> OnCommentBroadcast;
        public event Action<Payload_DOC_RELOAD_BROADCAST> OnDocReloadBroadcast;
        public event Action<Exception> OnConnectionLost;

        // ═══════════════════════════════════════════════════════════
        //  Connect / disconnect
        // ═══════════════════════════════════════════════════════════
        public void Connect(string host = "localhost", int port = 5000)
        {
            // Nếu đã đánh dấu connected nhưng socket thực sự đã chết → dọn dẹp trước
            if (_connected && !IsSocketAlive())
            {
                try { Disconnect(); } catch { }
            }

            if (_connected) return;

            _tcp = new TcpClient();
            _tcp.Connect(host, port);

            var ssl = new SslStream(
                _tcp.GetStream(),
                false,
                ValidateServerCertificate);

            ssl.AuthenticateAsClient(host, null, SslProtocols.Tls12, false);
            _stream = ssl;
            _connected = true;
            _stopReceive = false;

            _receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "MarkTogether-ReceiveLoop"
            };
            _receiveThread.Start();
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            string expectedThumb = LoadExpectedCertThumb();
            if (string.IsNullOrEmpty(expectedThumb))
                return true;

            return certificate != null
                && string.Equals(
                    certificate.GetCertHashString(),
                    expectedThumb.Replace(" ", "").Trim(),
                    StringComparison.OrdinalIgnoreCase);
        }

        private string LoadExpectedCertThumb()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.config");
                if (!File.Exists(path))
                    return string.Empty;

                foreach (var rawLine in File.ReadAllLines(path))
                {
                    string line = (rawLine ?? string.Empty).Trim();
                    if (line.StartsWith("CERT_THUMB=", StringComparison.OrdinalIgnoreCase))
                        return line.Substring("CERT_THUMB=".Length).Trim();
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        /// <summary>
        /// Kiểm tra socket TCP còn sống hay không (non-blocking poll).
        /// </summary>
        private bool IsSocketAlive()
        {
            try
            {
                if (_tcp == null || _tcp.Client == null || !_tcp.Client.Connected) return false;
                // Poll: nếu readable nhưng Available == 0 → peer đã đóng
                bool readable = _tcp.Client.Poll(0, System.Net.Sockets.SelectMode.SelectRead);
                if (readable && _tcp.Client.Available == 0) return false;
                return true;
            }
            catch { return false; }
        }

        public void Disconnect()
        {
            _stopReceive = true;
            _connected = false;
            Token = null;
            UserId = 0;
            Username = null;
            AvatarUrl = null;

            try { _stream?.Close(); } catch { }
            try { _tcp?.Close(); } catch { }

            // Fail mọi pending request
            foreach (var kv in _pending)
            {
                try { kv.Value.TrySetException(new IOException("Disconnected")); } catch { }
            }
            _pending.Clear();
        }

        // ═══════════════════════════════════════════════════════════
        //  Receive loop
        // ═══════════════════════════════════════════════════════════
        private void ReceiveLoop()
        {
            try
            {
                while (!_stopReceive && _connected)
                {
                    Packet packet = PacketHelper.Receive(_stream);
                    Dispatch(packet);
                }
            }
            catch (Exception ex) when (!_stopReceive)
            {
                OnConnectionLost?.Invoke(ex);
                _connected = false;

                foreach (var kv in _pending)
                {
                    try { kv.Value.TrySetException(ex); } catch { }
                }
                _pending.Clear();
            }
            catch { /* stop yêu cầu, im lặng */ }
        }

        private void Dispatch(Packet packet)
        {
            // Reply cho request đang đợi
            if (!string.IsNullOrEmpty(packet.RequestId)
                && _pending.TryRemove(packet.RequestId, out var tcs))
            {
                tcs.TrySetResult(packet);
                return;
            }

            // Push từ server
            try
            {
                switch (packet.Type)
                {
                    case MessageType.OP_BROADCAST:
                        OnOpBroadcast?.Invoke(packet.GetPayload<Payload_OP_BROADCAST>());
                        break;
                    case MessageType.CHAT_BROADCAST:
                        OnChatBroadcast?.Invoke(packet.GetPayload<Payload_CHAT_BROADCAST>());
                        break;
                    case MessageType.COMMENT_BROADCAST:
                        OnCommentBroadcast?.Invoke(packet.GetPayload<Payload_COMMENT_BROADCAST>());
                        break;
                    case MessageType.DOC_RELOAD_BROADCAST:
                        OnDocReloadBroadcast?.Invoke(packet.GetPayload<Payload_DOC_RELOAD_BROADCAST>());
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SocketClient] Dispatch error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Core: send-and-wait
        // ═══════════════════════════════════════════════════════════
        private Packet Request(MessageType type, object payload, int timeoutMs = 15000)
        {
            return RequestAsync(type, payload, timeoutMs).GetAwaiter().GetResult();
        }

        private async Task<Packet> RequestAsync(MessageType type, object payload, int timeoutMs = 15000)
        {
            EnsureConnected();

            var packet = Packet.Create(type, payload);
            packet.Token = Token;
            packet.RequestId = Guid.NewGuid().ToString("N");

            var tcs = new TaskCompletionSource<Packet>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[packet.RequestId] = tcs;

            try
            {
                PacketHelper.Send(_stream, packet);
            }
            catch
            {
                _pending.TryRemove(packet.RequestId, out _);
                throw;
            }

            using (var cts = new CancellationTokenSource(timeoutMs))
            {
                var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs, cts.Token))
                    .ConfigureAwait(false);

                if (completed != tcs.Task)
                {
                    _pending.TryRemove(packet.RequestId, out _);
                    throw new TimeoutException($"Server không phản hồi trong {timeoutMs}ms ({type}).");
                }

                cts.Cancel();
                return await tcs.Task.ConfigureAwait(false);
            }
        }

        private void EnsureConnected()
        {
            if (!_connected)
                throw new InvalidOperationException("Chưa kết nối tới server.");
        }

        private void EnsureAuthenticated()
        {
            EnsureConnected();
            if (string.IsNullOrEmpty(Token))
                throw new InvalidOperationException("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
        }

        private static T ExtractOrThrow<T>(Packet response, MessageType expected)
        {
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Lỗi không xác định.");
            }
            if (response.Type != expected)
            {
                throw new InvalidOperationException($"Phản hồi không hợp lệ: nhận {response.Type}, mong đợi {expected}.");
            }
            return response.GetPayload<T>();
        }

        // ═══════════════════════════════════════════════════════════
        //  AUTH
        // ═══════════════════════════════════════════════════════════
        public Payload_AUTH_RESPONSE Login(string username, string password)
        {
            var response = Request(MessageType.AUTH_LOGIN,
                new Payload_AUTH_LOGIN { Username = username, Password = password });
            var authResp = response.GetPayload<Payload_AUTH_RESPONSE>();

            if (authResp.Success)
            {
                Token = authResp.Token;
                UserId = authResp.UserId;
                Username = authResp.Username;
                AvatarUrl = authResp.AvatarUrl;
            }
            return authResp;
        }

        public Payload_AUTH_RESPONSE Register(string username, string email, string password)
        {
            var response = Request(MessageType.AUTH_REGISTER,
                new Payload_AUTH_REGISTER { Username = username, Email = email, Password = password });
            var authResp = response.GetPayload<Payload_AUTH_RESPONSE>();

            if (authResp.Success)
            {
                Token = authResp.Token;
                UserId = authResp.UserId;
                Username = authResp.Username;
            }
            return authResp;
        }

        public Payload_AUTH_LOGOUT_Response Logout()
        {
            if (!IsLoggedIn) return new Payload_AUTH_LOGOUT_Response { success = true };
            try
            {
                var response = Request(MessageType.AUTH_LOGOUT, new Payload_AUTH_LOGOUT_Request(), timeoutMs: 5000);
                return response.GetPayload<Payload_AUTH_LOGOUT_Response>()
                       ?? new Payload_AUTH_LOGOUT_Response { success = true };
            }
            catch
            {
                return new Payload_AUTH_LOGOUT_Response { success = true };
            }
            finally
            {
                Token = null;
                UserId = 0;
                Username = null;
                AvatarUrl = null;
            }
        }

        public Payload_AUTH_FORGOT_PASSWORD_Response RequestPasswordReset(string email)
        {
            EnsureConnected();
            var response = Request(MessageType.AUTH_FORGOT_PASSWORD,
                new Payload_AUTH_FORGOT_PASSWORD_Request { Email = email },
                timeoutMs: 30000);
            return ExtractOrThrow<Payload_AUTH_FORGOT_PASSWORD_Response>(response, MessageType.AUTH_FORGOT_PASSWORD);
        }

        public Payload_AUTH_RESET_PASSWORD_Response ResetPassword(string email, string otp, string newPassword)
        {
            EnsureConnected();
            var response = Request(MessageType.AUTH_RESET_PASSWORD,
                new Payload_AUTH_RESET_PASSWORD_Request { Email = email, Otp = otp, NewPassword = newPassword },
                timeoutMs: 15000);
            return ExtractOrThrow<Payload_AUTH_RESET_PASSWORD_Response>(response, MessageType.AUTH_RESET_PASSWORD);
        }

        // ═══════════════════════════════════════════════════════════
        //  DOCUMENT
        // ═══════════════════════════════════════════════════════════
        public Payload_DOC_LIST_Response GetDocuments()
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_LIST, new Payload_DOC_LIST_Request());
            var payload = ExtractOrThrow<Payload_DOC_LIST_Response>(response, MessageType.DOC_LIST)
                          ?? new Payload_DOC_LIST_Response();
            if (payload.documents == null) payload.documents = new List<DocInfo>();
            return payload;
        }

        public Payload_DOC_CREATE_Response CreateDocument(string title) => CreateDocument(title, string.Empty);

        public Payload_DOC_CREATE_Response CreateDocument(string title, string content)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_CREATE,
                new Payload_DOC_CREATE_Request { title = title, content = content });
            return ExtractOrThrow<Payload_DOC_CREATE_Response>(response, MessageType.DOC_CREATE);
        }

        public Payload_DOC_OPEN_Response OpenDocument(string docId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_OPEN, new Payload_DOC_OPEN_Request { docID = docId });
            return ExtractOrThrow<Payload_DOC_OPEN_Response>(response, MessageType.DOC_OPEN);
        }

        public void LeaveDocument(string docId)
        {
            EnsureAuthenticated();
            try
            {
                Request(MessageType.DOC_LEAVE, new Payload_DOC_LEAVE_Request { docID = docId }, timeoutMs: 5000);
            }
            catch { /* fire-and-forget */ }
        }

        public Payload_DOC_DELETE_Response DeleteDocument(string docId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_DELETE,
                new Payload_DOC_DELETE_Request { docID = docId });
            return ExtractOrThrow<Payload_DOC_DELETE_Response>(response, MessageType.DOC_DELETE);
        }

        public void SaveDocument(string docId, string content, string kind = "manual", int periodicMin = 0)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_SAVE,
                new Payload_DOC_SAVE_Request
                {
                    docID = docId,
                    content = content ?? string.Empty,
                    kind = kind,
                    periodicIntervalMin = periodicMin
                });
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Không thể lưu tài liệu.");
            }
            if (response.Type != MessageType.OK)
                throw new InvalidOperationException($"Phản hồi không hợp lệ khi lưu tài liệu: {response.Type}");
        }

        // ─── Real-time ops ───
        public void SendInsertOps(string docId, int clientRevision, List<EditOpItem> ops)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.OP_INSERT,
                new Payload_OP_INSERT
                { docID = docId, clientResivion = clientRevision, ops = ops ?? new List<EditOpItem>() },
                timeoutMs: 5000);
            EnsureOkResponse(response, MessageType.OP_INSERT);
        }

        public void SendDeleteOps(string docId, int clientRevision, List<EditOpItem> ops)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.OP_DELETE,
                new Payload_OP_DELETE
                { docID = docId, clientResivion = clientRevision, ops = ops ?? new List<EditOpItem>() },
                timeoutMs: 5000);
            EnsureOkResponse(response, MessageType.OP_DELETE);
        }

        private static void EnsureOkResponse(Packet response, MessageType origin)
        {
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? $"Gửi {origin} thất bại.");
            }
            if (response.Type != MessageType.OK)
                throw new InvalidOperationException($"Phản hồi không hợp lệ khi gửi {origin}: {response.Type}");
        }

        // ─── Sharing ───
        public Payload_DOC_SHARE_Response ShareDocument(string docId, string targetUsername, string permission)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_SHARE,
                new Payload_DOC_SHARE_Request
                { docID = docId, targetUsername = targetUsername, permission = permission });
            return ExtractOrThrow<Payload_DOC_SHARE_Response>(response, MessageType.DOC_SHARE);
        }

        public Payload_DOC_SHARE_LIST_Response ListCollaborators(string docId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_SHARE_LIST,
                new Payload_DOC_SHARE_LIST_Request { docID = docId });
            return ExtractOrThrow<Payload_DOC_SHARE_LIST_Response>(response, MessageType.DOC_SHARE_LIST);
        }

        public Payload_DOC_SHARE_UPDATE_Response UpdatePermission(string docId, int targetUserId, string newPermission)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_SHARE_UPDATE,
                new Payload_DOC_SHARE_UPDATE_Request
                { docID = docId, targetUserId = targetUserId, newPermission = newPermission });
            return ExtractOrThrow<Payload_DOC_SHARE_UPDATE_Response>(response, MessageType.DOC_SHARE_UPDATE);
        }

        public Payload_DOC_SHARE_REVOKE_Response RevokeAccess(string docId, int targetUserId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_SHARE_REVOKE,
                new Payload_DOC_SHARE_REVOKE_Request { docID = docId, targetUserId = targetUserId });
            return ExtractOrThrow<Payload_DOC_SHARE_REVOKE_Response>(response, MessageType.DOC_SHARE_REVOKE);
        }

        public Payload_DOC_SHARE_REGEN_CODE_Response RegenerateShareCode(string docId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_SHARE_REGEN_CODE,
                new Payload_DOC_SHARE_REGEN_CODE_Request { docID = docId });
            return ExtractOrThrow<Payload_DOC_SHARE_REGEN_CODE_Response>(response, MessageType.DOC_SHARE_REGEN_CODE);
        }

        public Payload_DOC_JOIN_CODE_Response JoinByShareCode(string code)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_JOIN_CODE,
                new Payload_DOC_JOIN_CODE_Request { shareCode = code });
            return ExtractOrThrow<Payload_DOC_JOIN_CODE_Response>(response, MessageType.DOC_JOIN_CODE);
        }

        // ─── Chat ───
        public Payload_CHAT_SEND_Response SendChat(string docId, string content)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.CHAT_SEND,
                new Payload_CHAT_SEND_Request { docID = docId, content = content });
            return ExtractOrThrow<Payload_CHAT_SEND_Response>(response, MessageType.CHAT_SEND);
        }

        public Payload_CHAT_HISTORY_Response GetChatHistory(string docId, int limit = 50)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.CHAT_HISTORY,
                new Payload_CHAT_HISTORY_Request { docID = docId, limit = limit });
            return ExtractOrThrow<Payload_CHAT_HISTORY_Response>(response, MessageType.CHAT_HISTORY);
        }

        // ─── Comment ───
        public Payload_COMMENT_CREATE_Response CreateComment(Payload_COMMENT_CREATE_Request req)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.COMMENT_CREATE, req);
            return ExtractOrThrow<Payload_COMMENT_CREATE_Response>(response, MessageType.COMMENT_CREATE);
        }

        public Payload_COMMENT_LIST_Response ListComments(string docId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.COMMENT_LIST, new Payload_COMMENT_LIST_Request { docID = docId });
            return ExtractOrThrow<Payload_COMMENT_LIST_Response>(response, MessageType.COMMENT_LIST);
        }

        public void ResolveComment(string commentId, bool resolved)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.COMMENT_RESOLVE,
                new Payload_COMMENT_RESOLVE_Request { commentId = commentId, resolved = resolved });
            EnsureOkResponse(response, MessageType.COMMENT_RESOLVE);
        }

        public void DeleteComment(string commentId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.COMMENT_DELETE,
                new Payload_COMMENT_DELETE_Request { commentId = commentId });
            EnsureOkResponse(response, MessageType.COMMENT_DELETE);
        }

        // ─── Image ───
        public Payload_IMAGE_UPLOAD_Response UploadImage(string fileName, string mimeType, byte[] data,
            bool isAvatar, string docId = null)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.IMAGE_UPLOAD,
                new Payload_IMAGE_UPLOAD_Request
                {
                    fileName = fileName,
                    mimeType = mimeType,
                    data = data,
                    isAvatar = isAvatar,
                    docID = docId
                },
                timeoutMs: 60000);
            return ExtractOrThrow<Payload_IMAGE_UPLOAD_Response>(response, MessageType.IMAGE_UPLOAD);
        }

        public Payload_IMAGE_GET_Response GetImage(string imageId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.IMAGE_GET,
                new Payload_IMAGE_GET_Request { imageId = imageId },
                timeoutMs: 30000);
            return ExtractOrThrow<Payload_IMAGE_GET_Response>(response, MessageType.IMAGE_GET);
        }

        // ─── Version ───
        public Payload_DOC_VERSION_LIST_Response GetVersions(string docId, int page = 1, int limit = 20)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_VERSION_LIST,
                new Payload_DOC_VERSION_LIST_Request { docID = docId, page = page, limit = limit });
            return ExtractOrThrow<Payload_DOC_VERSION_LIST_Response>(response, MessageType.DOC_VERSION_LIST);
        }

        public Payload_DOC_VERSION_DETAIL_Response GetVersionDetail(string versionId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_VERSION_DETAIL,
                new Payload_DOC_VERSION_DETAIL_Request { versionId = versionId });
            return ExtractOrThrow<Payload_DOC_VERSION_DETAIL_Response>(response, MessageType.DOC_VERSION_DETAIL);
        }

        public Payload_DOC_VERSION_RESTORE_Response RestoreVersion(string docId, string versionId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_VERSION_RESTORE,
                new Payload_DOC_VERSION_RESTORE_Request { docID = docId, versionId = versionId });
            return ExtractOrThrow<Payload_DOC_VERSION_RESTORE_Response>(response, MessageType.DOC_VERSION_RESTORE);
        }

        public Payload_DOC_VERSION_DELETE_Response DeleteVersion(string versionId)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.DOC_VERSION_DELETE,
                new Payload_DOC_VERSION_DELETE_Request { versionId = versionId });
            return ExtractOrThrow<Payload_DOC_VERSION_DELETE_Response>(response, MessageType.DOC_VERSION_DELETE);
        }

        // ─── AI ───
        public Payload_AI_Response AskAI(
            string mode, string userPrompt, string contextText,
            string docId = null,
            string apiKey = null, string model = null, string provider = null,
            string actionMode = null,
            string documentText = null,
            int selectionStart = 0, int selectionEnd = 0, int cursorPosition = 0)
        {
            EnsureAuthenticated();
            var response = Request(MessageType.AI_REQUEST,
                new Payload_AI_Request
                {
                    docID = docId,
                    mode = mode,
                    userPrompt = userPrompt,
                    contextText = contextText,
                    apiKey = apiKey,
                    model = model,
                    provider = provider,
                    actionMode = actionMode,
                    documentText = documentText,
                    selectionStart = selectionStart,
                    selectionEnd = selectionEnd,
                    cursorPosition = cursorPosition
                },
                timeoutMs: 60000);
            return ExtractOrThrow<Payload_AI_Response>(response, MessageType.AI_RESPONSE);
        }

        // ─── Raw send (cho debug) ───
        public void Send(Packet packet)
        {
            EnsureConnected();
            PacketHelper.Send(_stream, packet);
        }
    }
}