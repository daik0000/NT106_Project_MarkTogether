using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using MarkTogether.Shared;

namespace MarkTogether.Client.Network
{
    /// <summary>
    /// Singleton TCP client: giữ kết nối xuyên suốt phiên làm việc.
    /// Cung cấp hàm Login/Register tiện lợi.
    /// </summary>
    public class SocketClient
    {
        private TcpClient _tcp;
        private NetworkStream _stream;
        private bool _connected;
        private readonly object _requestSync = new object();
        private volatile bool _waitingForResponse = false; // [ADDED] Track in-flight requests

        // [ADDED] Background listener and response queue
        // [FIX] Using unbounded collection to ensure no legitimate responses are dropped.
        private readonly BlockingCollection<Packet> _responseQueue = new BlockingCollection<Packet>();
        private Thread _listenerThread;

        // Singleton instance
        public static SocketClient Instance { get; } = new SocketClient();

        // Thông tin user sau khi login thành công
        public string Token { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        // [ADDED] Event for server broadcast
        public event Action<Payload_OP_BROADCAST> BroadcastReceived;

        // [ADDED] Event for revision ACKs
        public event Action<int> RevisionAckReceived;

        // [ADDED] Persistent logging helper
        private static void ClientLog(string msg)
        {
            Logger.Log(msg);
        }

        /// <summary>
        /// Kết nối đến server.
        /// </summary>
        public void Connect()
        {
            if (_connected) return; // Đã kết nối rồi thì bỏ qua

            string host = "localhost";
            int port = 5000;

            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.config");
                if (System.IO.File.Exists(configPath))
                {
                    var lines = System.IO.File.ReadAllLines(configPath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim().ToUpper();
                            string val = parts[1].Trim();
                            if (key == "HOST") host = val;
                            else if (key == "PORT") int.TryParse(val, out port);
                        }
                    }
                }
            }
            catch { /* Fallback to default */ }

            _tcp = new TcpClient();
            _tcp.Connect(host, port);
            _stream = _tcp.GetStream();
            _connected = true;

            // [ADDED] Start background listener thread
            StartListenerThread();
        }

        // [ADDED] Listener thread implementation
        private void StartListenerThread()
        {
            _listenerThread = new Thread(() =>
            {
                while (_connected)
                {
                    try
                    {
                        Packet packet = PacketHelper.Receive(_stream);
                        if (packet == null) continue;

                        ClientLog($"[Listener] Packet received: {packet.Type}");

                        if (packet.Type == MessageType.OP_BROADCAST)
                        {
                            var payload = packet.GetPayload<Payload_OP_BROADCAST>();
                            ClientLog($"[Broadcast] docId={payload?.docID} opType={payload?.opType} listeners={BroadcastReceived?.GetInvocationList()?.Length ?? 0}");
                            BroadcastReceived?.Invoke(payload);
                        }
                        else if (packet.Type == MessageType.OK && int.TryParse(packet.GetPayload<Payload_OK>()?.Message, out int serverRev))
                        {
                            // [FIX] Handle numeric OK as a revision ACK from OP_INSERT/OP_DELETE
                            RevisionAckReceived?.Invoke(serverRev);
                        }
                        else if (_waitingForResponse) // [FIX] Only queue if we expect a response
                        {
                            _responseQueue.Add(packet);
                        }
                        else
                        {
                            // Unexpected packet (e.g. OK from DOC_SAVE/DOC_LEAVE) — log and discard
                            ClientLog($"[Listener] Discarding unexpected packet: {packet.Type}");
                        }
                    }
                    catch (Exception)
                    {
                        // IOException is expected when stream is closed in Disconnect()
                        if (_connected)
                        {
                            _connected = false;
                        }
                    }
                }
            })
            {
                IsBackground = true,
                Name = "SocketClientListener"
            };
            _listenerThread.Start();
        }

        /// <summary>
        /// Gửi packet đến server.
        /// </summary>
        public void Send(Packet packet)
        {
            PacketHelper.Send(_stream, packet);
        }

        /// <summary>
        /// Nhận packet từ server (blocking).
        /// </summary>
        public Packet Receive()
        {
            // [MODIFIED] Use queue instead of direct receive
            return _responseQueue.Take();
        }

        /// <summary>
        /// Đăng nhập. Trả Payload_AUTH_RESPONSE.
        /// </summary>
        public Payload_AUTH_RESPONSE Login(string username, string password)
        {
            var packet = Packet.Create(MessageType.AUTH_LOGIN,
                new Payload_AUTH_LOGIN { Username = username, Password = password });
            Packet response = SendAndReceive(packet);
            var authResp = response.GetPayload<Payload_AUTH_RESPONSE>();

            if (authResp.Success)
            {
                Token = authResp.Token;
                UserId = authResp.UserId;
                Username = authResp.Username;
            }

            return authResp;
        }

        /// <summary>
        /// Đăng ký. Trả Payload_AUTH_RESPONSE.
        /// </summary>
        public Payload_AUTH_RESPONSE Register(string username, string email, string password)
        {
            var packet = Packet.Create(MessageType.AUTH_REGISTER,
                new Payload_AUTH_REGISTER
                {
                    Username = username,
                    Email = email,
                    Password = password
                });
            Packet response = SendAndReceive(packet);
            var authResp = response.GetPayload<Payload_AUTH_RESPONSE>();

            if (authResp.Success)
            {
                Token = authResp.Token;
                UserId = authResp.UserId;
                Username = authResp.Username;
            }

            return authResp;
        }

        public Payload_DOC_LIST_Response GetDocuments()
        {
            EnsureAuthenticated();

            var packet = Packet.Create(MessageType.DOC_LIST, new Payload_DOC_LIST_Request());
            packet.Token = Token;
            Packet response = SendAndReceive(packet);
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Không thể tải danh sách tài liệu.");
            }

            if (response.Type != MessageType.DOC_LIST)
            {
                throw new InvalidOperationException($"Phản hồi không hợp lệ khi tải danh sách tài liệu: {response.Type}");
            }

            var payload = response.GetPayload<Payload_DOC_LIST_Response>() ?? new Payload_DOC_LIST_Response();
            if (payload.documents == null)
            {
                payload.documents = new List<DocInfo>();
            }

            return payload;
        }

        public Payload_DOC_CREATE_Response CreateDocument(string title)
        {
            return CreateDocument(title, string.Empty);
        }

        public Payload_DOC_CREATE_Response CreateDocument(string title, string content)
        {
            EnsureAuthenticated();

            var packet = Packet.Create(MessageType.DOC_CREATE,
                new Payload_DOC_CREATE_Request
                {
                    title = title,
                    content = content
                });
            packet.Token = Token;
            Packet response = SendAndReceive(packet);
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Không thể tạo tài liệu mới.");
            }

            if (response.Type != MessageType.DOC_CREATE)
            {
                throw new InvalidOperationException($"Phản hồi không hợp lệ khi tạo tài liệu: {response.Type}");
            }

            return response.GetPayload<Payload_DOC_CREATE_Response>();
        }

        public Payload_DOC_OPEN_Response OpenDocument(string docId)
        {
            EnsureAuthenticated();

            var packet = Packet.Create(MessageType.DOC_OPEN,
                new Payload_DOC_OPEN_Request { docID = docId });
            packet.Token = Token;
            Packet response = SendAndReceive(packet);
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Không thể mở tài liệu.");
            }

            if (response.Type != MessageType.DOC_OPEN)
            {
                throw new InvalidOperationException($"Phản hồi không hợp lệ khi mở tài liệu: {response.Type}");
            }

            return response.GetPayload<Payload_DOC_OPEN_Response>();
        }

        public void SaveDocument(string docId, string content)
        {
            EnsureAuthenticated();

            var packet = Packet.Create(MessageType.DOC_SAVE,
                new Payload_DOC_SAVE_Request
                {
                    docID = docId,
                    content = content ?? string.Empty
                });
            packet.Token = Token;
            Packet response = SendAndReceive(packet);
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Không thể lưu tài liệu.");
            }

            if (response.Type != MessageType.OK)
            {
                throw new InvalidOperationException($"Phản hồi không hợp lệ khi lưu tài liệu: {response.Type}");
            }
        }

        // [ADDED] Leave document
        public void LeaveDocument(string docId)
        {
            if (string.IsNullOrEmpty(Token)) return;

            var packet = Packet.Create(MessageType.DOC_LEAVE,
                new Payload_DOC_LEAVE_Request { docID = docId });
            packet.Token = Token;
            try
            {
                SendAndReceive(packet);
            }
            catch { /* Ignore errors on leave */ }
        }

        // [ADDED] Share document with permission
        public Payload_DOC_SHARE_Response ShareDocument(string docId, string targetUsername, string permission)
        {
            EnsureAuthenticated();

            var packet = Packet.Create(MessageType.DOC_SHARE,
                new Payload_DOC_SHARE_Request
                {
                    docID = docId,
                    targetUsername = targetUsername,
                    Permission = permission
                });
            packet.Token = Token;
            Packet response = SendAndReceive(packet);
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Không thể chia sẻ tài liệu.");
            }

            return response.GetPayload<Payload_DOC_SHARE_Response>();
        }

        // [ADDED] Logout and clear session
        public void Logout()
        {
            Disconnect();
            Token = null;
            UserId = -1;
            Username = null;
        }

        // [ADDED] Get share info
        public Payload_DOC_GET_SHARES_Response GetShareInfo(string docId)
        {
            EnsureAuthenticated();
            var packet = Packet.Create(MessageType.DOC_GET_SHARES, new Payload_DOC_GET_SHARES_Request { docID = docId });
            packet.Token = Token;
            Packet response = SendAndReceive(packet);
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Không thể lấy thông tin chia sẻ.");
            }
            return response.GetPayload<Payload_DOC_GET_SHARES_Response>();
        }

        // [ADDED] Revoke access
        public void RevokeAccess(string docId, int targetUserId)
        {
            EnsureAuthenticated();
            var packet = Packet.Create(MessageType.DOC_REVOKE, new Payload_DOC_REVOKE_Request { docID = docId, targetUserId = targetUserId });
            packet.Token = Token;
            Packet response = SendAndReceive(packet);
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Không thể thu hồi quyền truy cập.");
            }
        }

        // [ADDED] Update user permission
        public void UpdatePermission(string docId, int targetUserId, string permission)
        {
            EnsureAuthenticated();
            var packet = Packet.Create(MessageType.DOC_UPDATE_PERM, new Payload_DOC_UPDATE_PERM_Request { docID = docId, targetUserId = targetUserId, permission = permission });
            packet.Token = Token;
            Packet response = SendAndReceive(packet);
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Không thể cập nhật quyền truy cập.");
            }
        }

        // [ADDED] Set public access
        public void SetPublic(string docId, bool isPublic, string publicPermission)
        {
            EnsureAuthenticated();
            var packet = Packet.Create(MessageType.DOC_SET_PUBLIC, new Payload_DOC_SET_PUBLIC_Request { docID = docId, isPublic = isPublic, publicPermission = publicPermission });
            packet.Token = Token;
            Packet response = SendAndReceive(packet);
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Không thể cập nhật cài đặt công khai.");
            }
        }

        // [ADDED] Join by code
        public Payload_DOC_JOIN_CODE_Response JoinByCode(string shareCode)
        {
            EnsureAuthenticated();

            var packet = Packet.Create(MessageType.DOC_JOIN_CODE,
                new Payload_DOC_JOIN_CODE_Request { shareCode = shareCode });
            packet.Token = Token;
            Packet response = SendAndReceive(packet);
            if (response.Type == MessageType.ERROR)
            {
                var err = response.GetPayload<Payload_ERROR>();
                throw new InvalidOperationException(err?.Message ?? "Mã chia sẻ không hợp lệ.");
            }

            return response.GetPayload<Payload_DOC_JOIN_CODE_Response>();
        }

        public void SendInsertOps(string docId, int clientRevision, List<EditOpItem> ops)
        {
            EnsureAuthenticated();

            var payload = new Payload_OP_INSERT
            {
                docID = docId,
                clientResivion = clientRevision,
                ops = ops ?? new List<EditOpItem>()
            };

            SendRealtimeOpPacket(MessageType.OP_INSERT, payload);
        }

        public void SendDeleteOps(string docId, int clientRevision, List<EditOpItem> ops)
        {
            EnsureAuthenticated();

            var payload = new Payload_OP_DELETE
            {
                docID = docId,
                clientResivion = clientRevision,
                ops = ops ?? new List<EditOpItem>()
            };

            SendRealtimeOpPacket(MessageType.OP_DELETE, payload);
        }

        private void SendRealtimeOpPacket(MessageType type, object payload)
        {
            var packet = Packet.Create(type, payload);
            packet.Token = Token;
            
            // [FIX] Real-time ops are now FIRE-AND-FORGET to prevent deadlocks and lag.
            // We only lock for the duration of the send operation.
            lock (_requestSync)
            {
                Send(packet);
            }
        }

        /// <summary>
        /// Ngắt kết nối.
        /// </summary>
        public void Disconnect()
        {
            // [VERIFIED] Listener thread is properly stopped by setting _connected to false 
            // and closing the stream which triggers an exception in PacketHelper.Receive.
            _connected = false;
            Token = null;

            try
            {
                _stream?.Close();
                _tcp?.Close();
            }
            catch { /* Ignore close errors */ }

            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                // Give it a moment to exit
                _listenerThread.Join(500);
            }

            // [ADDED] Clear response queue
            while (_responseQueue.Count > 0) _responseQueue.TryTake(out _);
        }

        private Packet SendAndReceive(Packet packet)
        {
            lock (_requestSync)
            {
                _waitingForResponse = true; // [FIX] Start expecting
                try
                {
                    Send(packet);
                    return Receive();
                }
                finally
                {
                    _waitingForResponse = false; // [FIX] Stop expecting
                }
            }
        }

        private void EnsureAuthenticated()
        {
            if (!_connected || string.IsNullOrEmpty(Token))
            {
                throw new InvalidOperationException("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
            }
        }
    }
}
