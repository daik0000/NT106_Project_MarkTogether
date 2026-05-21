using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using MarkTogether.Server.Database.Models;
using MarkTogether.Server.Database.Repositories;
using MarkTogether.Server.Services;
using MarkTogether.Shared;

namespace MarkTogether.Server.Network
{
    /// <summary>
    /// Mỗi client TCP có 1 ClientHandler riêng chạy trên thread riêng.
    /// </summary>
    public class ClientHandler
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private string _token;
        private int _userId = -1;
        private string _username;

        // Throttle AI request: 1 req / 3 giây / user
        private DateTime _lastAiRequestUtc = DateTime.MinValue;

        public int UserId => _userId;
        public string Username => _username;
        public string Token => _token;

        public ClientHandler(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
        }

        // ═══════════════════════════════════════════════════════════
        //  Public Send (cho SessionManager broadcast push)
        // ═══════════════════════════════════════════════════════════
        public void SendSafe(Packet packet)
        {
            try
            {
                if (_tcpClient != null && _tcpClient.Connected)
                {
                    PacketHelper.Send(_stream, packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handler] SendSafe failed for user {_username}: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  MAIN LOOP
        // ═══════════════════════════════════════════════════════════
        public void ProcessAsync()
        {
            try
            {
                while (_tcpClient.Connected)
                {
                    Packet packet = PacketHelper.Receive(_stream);

                    switch (packet.Type)
                    {
                        case MessageType.AUTH_REGISTER: HandleRegister(packet); break;
                        case MessageType.AUTH_LOGIN: HandleLogin(packet); break;
                        case MessageType.AUTH_LOGOUT: HandleLogout(packet); break;

                        case MessageType.DOC_LIST: HandleDocList(packet); break;
                        case MessageType.DOC_CREATE: HandleDocCreate(packet); break;
                        case MessageType.DOC_OPEN: HandleDocOpen(packet); break;
                        case MessageType.DOC_SAVE: HandleDocSave(packet); break;
                        case MessageType.DOC_LEAVE: HandleDocLeave(packet); break;
                        case MessageType.DOC_JOIN_CODE: HandleDocJoinCode(packet); break;

                        case MessageType.DOC_SHARE: HandleDocShare(packet); break;
                        case MessageType.DOC_SHARE_LIST: HandleDocShareList(packet); break;
                        case MessageType.DOC_SHARE_UPDATE: HandleDocShareUpdate(packet); break;
                        case MessageType.DOC_SHARE_REVOKE: HandleDocShareRevoke(packet); break;
                        case MessageType.DOC_SHARE_REGEN_CODE: HandleDocShareRegenCode(packet); break;

                        case MessageType.OP_INSERT: HandleOpInsert(packet); break;
                        case MessageType.OP_DELETE: HandleOpDelete(packet); break;

                        case MessageType.CHAT_SEND: HandleChatSend(packet); break;
                        case MessageType.CHAT_HISTORY: HandleChatHistory(packet); break;

                        case MessageType.COMMENT_CREATE: HandleCommentCreate(packet); break;
                        case MessageType.COMMENT_LIST: HandleCommentList(packet); break;
                        case MessageType.COMMENT_RESOLVE: HandleCommentResolve(packet); break;
                        case MessageType.COMMENT_DELETE: HandleCommentDelete(packet); break;

                        case MessageType.IMAGE_UPLOAD: HandleImageUpload(packet); break;
                        case MessageType.IMAGE_GET: HandleImageGet(packet); break;

                        case MessageType.DOC_VERSION_LIST: HandleVersionList(packet); break;
                        case MessageType.DOC_VERSION_DETAIL: HandleVersionDetail(packet); break;
                        case MessageType.DOC_VERSION_RESTORE: HandleVersionRestore(packet); break;
                        case MessageType.DOC_VERSION_DELETE: HandleVersionDelete(packet); break;

                        case MessageType.AI_REQUEST: HandleAiRequest(packet); break;

                        default:
                            if (_userId < 0)
                            {
                                ReplyError(packet, "Bạn chưa đăng nhập. Vui lòng login trước.");
                                break;
                            }
                            ReplyError(packet, $"MessageType '{packet.Type}' chưa được hỗ trợ.");
                            break;
                    }
                }
            }
            catch (IOException)
            {
                Console.WriteLine($"[Handler] Client {_username ?? "unknown"} (ID={_userId}) đã ngắt kết nối.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handler] Lỗi: {ex}");
            }
            finally
            {
                SessionManager.LeaveAllRooms(this);
                if (!string.IsNullOrEmpty(_token))
                    SessionManager.RemoveSession(_token);

                _stream?.Close();
                _tcpClient?.Close();

                Console.WriteLine($"[Handler] Đã dọn dẹp kết nối cho user {_username ?? "unknown"}.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════════
        private int ResolveCurrentUserId(Packet packet)
        {
            if (!string.IsNullOrWhiteSpace(packet?.Token))
            {
                int tokenUserId = SessionManager.GetUserId(packet.Token);
                if (tokenUserId > 0) return tokenUserId;
            }
            return _userId;
        }

        private void Reply(Packet origin, MessageType type, object payload)
        {
            var p = Packet.Create(type, payload);
            p.RequestId = origin?.RequestId;
            PacketHelper.Send(_stream, p);
        }

        private void ReplyOk(Packet origin, string message = "OK")
        {
            Reply(origin, MessageType.OK, new Payload_OK { Message = message });
        }

        private void ReplyError(Packet origin, string message)
        {
            Reply(origin, MessageType.ERROR, new Payload_ERROR { Message = message });
        }

        /// <summary>
        /// Trả permission của user trên doc, hoặc null nếu không có quyền.
        /// </summary>
        private string GetPermission(string docId, int userId)
        {
            return DocumentShareRepository.GetPermission(docId, userId);
        }

        // ═══════════════════════════════════════════════════════════
        //  AUTH
        // ═══════════════════════════════════════════════════════════
        private void HandleRegister(Packet packet)
        {
            var payload = packet.GetPayload<Payload_AUTH_REGISTER>();
            var response = AuthService.Register(payload.Username, payload.Email, payload.Password);

            if (response.Success)
            {
                _token = response.Token;
                _userId = response.UserId;
                _username = response.Username;
                SessionManager.AddSession(_token, _userId);
            }
            Reply(packet, MessageType.AUTH_RESPONSE, response);
        }

        private void HandleLogin(Packet packet)
        {
            var payload = packet.GetPayload<Payload_AUTH_LOGIN>();
            var response = AuthService.Login(payload.Username, payload.Password);

            if (response.Success)
            {
                _token = response.Token;
                _userId = response.UserId;
                _username = response.Username;
                SessionManager.AddSession(_token, _userId);

                var user = UserRepository.GetById(_userId);
                if (user != null) response.AvatarUrl = user.AvatarUrl;
            }
            Reply(packet, MessageType.AUTH_RESPONSE, response);
        }

        private void HandleLogout(Packet packet)
        {
            SessionManager.LeaveAllRooms(this);
            if (!string.IsNullOrEmpty(_token))
                SessionManager.RemoveSession(_token);

            _token = null;
            _userId = -1;
            _username = null;

            Reply(packet, MessageType.AUTH_LOGOUT, new Payload_AUTH_LOGOUT_Response
            {
                success = true,
                message = "Đăng xuất thành công"
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  DOCUMENT
        // ═══════════════════════════════════════════════════════════
        private void HandleDocList(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var docs = DocumentRepository.GetByUserIdWithPermission(currentUserId);
            var response = new Payload_DOC_LIST_Response
            {
                documents = docs.Select(d => new DocInfo
                {
                    docID = d.Id,
                    title = d.Title,
                    permission = d.Permission,
                    updateAt = d.UpdatedAt
                }).ToList()
            };
            Reply(packet, MessageType.DOC_LIST, response);
        }

        private void HandleDocCreate(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var payload = packet.GetPayload<Payload_DOC_CREATE_Request>();
            string title = (payload?.title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title)) title = "Tài liệu không tiêu đề";

            string shareCode = ShareCodeGenerator.GenerateUnique();

            var document = new Document
            {
                OwnerId = currentUserId,
                Title = title,
                Content = payload?.content ?? string.Empty,
                ShareCode = shareCode
            };
            string docId = DocumentRepository.Create(document);
            var created = DocumentRepository.GetById(docId);

            Reply(packet, MessageType.DOC_CREATE, new Payload_DOC_CREATE_Response
            {
                docID = docId,
                title = created?.Title ?? title,
                content = created?.Content ?? string.Empty,
                revision = DocumentRepository.GetCurrentRevision(docId),
                shareCode = created?.ShareCode ?? shareCode
            });
        }

        private void HandleDocOpen(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var payload = packet.GetPayload<Payload_DOC_OPEN_Request>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID))
            {
                ReplyError(packet, "docID không hợp lệ.");
                return;
            }

            string docId = payload.docID.Trim();
            var document = DocumentRepository.GetById(docId);
            if (document == null) { ReplyError(packet, "Không tìm thấy tài liệu."); return; }

            string permission = GetPermission(docId, currentUserId);
            if (permission == null) { ReplyError(packet, "Bạn không có quyền truy cập tài liệu này."); return; }

            // Owner thấy share code, viewer/editor không cần
            string shareCode = permission == "owner" ? document.ShareCode : null;

            SessionManager.JoinRoom(docId, this);

            Reply(packet, MessageType.DOC_OPEN, new Payload_DOC_OPEN_Response
            {
                docID = document.Id,
                title = document.Title,
                content = document.Content ?? string.Empty,
                revision = DocumentRepository.GetCurrentRevision(docId),
                permission = permission,
                shareCode = shareCode
            });
        }

        private void HandleDocLeave(Packet packet)
        {
            var payload = packet.GetPayload<Payload_DOC_LEAVE_Request>();
            if (payload != null && !string.IsNullOrWhiteSpace(payload.docID))
            {
                SessionManager.LeaveRoom(payload.docID, this);
            }
            Reply(packet, MessageType.DOC_LEAVE, new Payload_DOC_LEAVE_Response
            {
                success = true,
                message = "Đã rời tài liệu"
            });
        }

        private void HandleDocSave(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var payload = packet.GetPayload<Payload_DOC_SAVE_Request>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID))
            {
                ReplyError(packet, "docID không hợp lệ.");
                return;
            }

            string docId = payload.docID.Trim();
            var document = DocumentRepository.GetById(docId);
            if (document == null) { ReplyError(packet, "Không tìm thấy tài liệu."); return; }

            string permission = GetPermission(docId, currentUserId);
            if (permission != "owner" && permission != "editor")
            {
                ReplyError(packet, "Bạn không có quyền lưu tài liệu này.");
                return;
            }

            string newContent = payload.content ?? string.Empty;
            if (!DocumentRepository.UpdateContent(docId, newContent))
            {
                ReplyError(packet, "Lưu tài liệu thất bại.");
                return;
            }

            // CN9: tự lưu version mỗi lần save
            try
            {
                DocumentVersionRepository.SaveVersion(new DocumentVersion
                {
                    DocId = docId,
                    ContentSnapshot = newContent,
                    SavedBy = currentUserId,
                    Label = $"Saved by {_username}"
                });
                TrimVersions(docId, 50);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handler] Lưu version thất bại: {ex.Message}");
            }

            ReplyOk(packet, "Lưu tài liệu thành công");
        }

        private void TrimVersions(string docId, int keep)
        {
            try
            {
                int total = DocumentVersionRepository.CountVersions(docId);
                if (total <= keep) return;

                // Lấy versions cũ hơn để xoá
                var page = DocumentVersionRepository.GetVersions(docId, page: 1, limit: total);
                int toRemove = total - keep;
                // page sắp xếp DESC (mới nhất trước) → cuối list là cũ nhất
                for (int i = page.Count - 1; i >= 0 && toRemove > 0; i--, toRemove--)
                {
                    string id = page[i].id?.ToString();
                    if (!string.IsNullOrEmpty(id))
                        DocumentVersionRepository.DeleteVersion(id);
                }
            }
            catch { /* best-effort */ }
        }

        // ═══════════════════════════════════════════════════════════
        //  SHARE
        // ═══════════════════════════════════════════════════════════
        private void HandleDocShare(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_SHARE_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID) || string.IsNullOrWhiteSpace(p.targetUsername))
            {
                Reply(packet, MessageType.DOC_SHARE, new Payload_DOC_SHARE_Response
                { success = false, message = "Thiếu docID hoặc username." });
                return;
            }

            var doc = DocumentRepository.GetById(p.docID);
            if (doc == null || doc.OwnerId != currentUserId)
            {
                Reply(packet, MessageType.DOC_SHARE, new Payload_DOC_SHARE_Response
                { success = false, message = "Chỉ chủ sở hữu mới được chia sẻ." });
                return;
            }

            var target = UserRepository.GetByUsername(p.targetUsername.Trim());
            if (target == null)
            {
                Reply(packet, MessageType.DOC_SHARE, new Payload_DOC_SHARE_Response
                { success = false, message = "Không tìm thấy username." });
                return;
            }
            if (target.Id == currentUserId)
            {
                Reply(packet, MessageType.DOC_SHARE, new Payload_DOC_SHARE_Response
                { success = false, message = "Không thể chia sẻ với chính mình." });
                return;
            }

            string permission = (p.permission ?? "viewer").ToLowerInvariant();
            if (permission != "viewer" && permission != "editor") permission = "viewer";

            try
            {
                // Nếu đã có share rồi → update permission
                if (DocumentShareRepository.GetPermission(p.docID, target.Id) != null)
                {
                    DocumentShareRepository.UpdatePermission(p.docID, target.Id, permission);
                }
                else
                {
                    DocumentShareRepository.ShareDocument(p.docID, target.Id, permission);
                }

                Reply(packet, MessageType.DOC_SHARE, new Payload_DOC_SHARE_Response
                { success = true, message = $"Đã chia sẻ với {target.Username} ({permission})." });
            }
            catch (Exception ex)
            {
                Reply(packet, MessageType.DOC_SHARE, new Payload_DOC_SHARE_Response
                { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        private void HandleDocShareList(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_SHARE_LIST_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID))
            { ReplyError(packet, "docID không hợp lệ."); return; }

            var doc = DocumentRepository.GetById(p.docID);
            if (doc == null) { ReplyError(packet, "Không tìm thấy tài liệu."); return; }
            if (doc.OwnerId != currentUserId)
            {
                Reply(packet, MessageType.DOC_SHARE_LIST, new Payload_DOC_SHARE_LIST_Response
                { success = false, message = "Chỉ chủ sở hữu mới xem được danh sách." });
                return;
            }

            var rows = DocumentShareRepository.GetCollaborators(p.docID);
            var collaborators = new List<CollaboratorDto>();
            foreach (var r in rows)
            {
                collaborators.Add(new CollaboratorDto
                {
                    userId = (int)r.id,
                    username = (string)r.username,
                    email = r.email,
                    avatarUrl = r.avatar_url,
                    permission = (string)r.permission,
                    invitedAt = r.invited_at
                });
            }

            Reply(packet, MessageType.DOC_SHARE_LIST, new Payload_DOC_SHARE_LIST_Response
            {
                success = true,
                docID = p.docID,
                shareCode = doc.ShareCode,
                collaborators = collaborators
            });
        }

        private void HandleDocShareUpdate(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_SHARE_UPDATE_Request>();
            if (p == null) { ReplyError(packet, "Payload không hợp lệ."); return; }

            var doc = DocumentRepository.GetById(p.docID);
            if (doc == null || doc.OwnerId != currentUserId)
            {
                Reply(packet, MessageType.DOC_SHARE_UPDATE, new Payload_DOC_SHARE_UPDATE_Response
                { success = false, message = "Không có quyền." });
                return;
            }

            string perm = (p.newPermission ?? "viewer").ToLowerInvariant();
            if (perm != "viewer" && perm != "editor") perm = "viewer";

            bool ok = DocumentShareRepository.UpdatePermission(p.docID, p.targetUserId, perm);
            Reply(packet, MessageType.DOC_SHARE_UPDATE, new Payload_DOC_SHARE_UPDATE_Response
            {
                success = ok,
                message = ok ? "Cập nhật quyền thành công" : "Không có chia sẻ phù hợp để cập nhật"
            });
        }

        private void HandleDocShareRevoke(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_SHARE_REVOKE_Request>();
            if (p == null) { ReplyError(packet, "Payload không hợp lệ."); return; }

            var doc = DocumentRepository.GetById(p.docID);
            if (doc == null || doc.OwnerId != currentUserId)
            {
                Reply(packet, MessageType.DOC_SHARE_REVOKE, new Payload_DOC_SHARE_REVOKE_Response
                { success = false, message = "Không có quyền." });
                return;
            }

            bool ok = DocumentShareRepository.RevokeAccess(p.docID, p.targetUserId);
            Reply(packet, MessageType.DOC_SHARE_REVOKE, new Payload_DOC_SHARE_REVOKE_Response
            {
                success = ok,
                message = ok ? "Đã thu hồi quyền" : "Không có chia sẻ phù hợp"
            });
        }

        private void HandleDocShareRegenCode(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_SHARE_REGEN_CODE_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID))
            { ReplyError(packet, "docID không hợp lệ."); return; }

            var doc = DocumentRepository.GetById(p.docID);
            if (doc == null || doc.OwnerId != currentUserId)
            {
                Reply(packet, MessageType.DOC_SHARE_REGEN_CODE, new Payload_DOC_SHARE_REGEN_CODE_Response
                { success = false, message = "Không có quyền." });
                return;
            }

            string newCode = ShareCodeGenerator.GenerateUnique();
            DocumentRepository.UpdateShareCode(p.docID, newCode);

            Reply(packet, MessageType.DOC_SHARE_REGEN_CODE, new Payload_DOC_SHARE_REGEN_CODE_Response
            {
                success = true,
                shareCode = newCode,
                message = "Đã tạo mã chia sẻ mới"
            });
        }

        private void HandleDocJoinCode(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_JOIN_CODE_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.shareCode))
            {
                Reply(packet, MessageType.DOC_JOIN_CODE, new Payload_DOC_JOIN_CODE_Response
                { success = false, message = "Vui lòng nhập mã chia sẻ." });
                return;
            }

            var doc = DocumentRepository.GetByShareCode(p.shareCode.Trim());
            if (doc == null)
            {
                Reply(packet, MessageType.DOC_JOIN_CODE, new Payload_DOC_JOIN_CODE_Response
                { success = false, message = "Mã chia sẻ không tồn tại." });
                return;
            }

            string perm = GetPermission(doc.Id, currentUserId);
            if (perm == null)
            {
                // Tạo bản ghi share viewer nếu chưa có
                DocumentShareRepository.ShareDocument(doc.Id, currentUserId, "viewer");
                perm = "viewer";
            }

            Reply(packet, MessageType.DOC_JOIN_CODE, new Payload_DOC_JOIN_CODE_Response
            {
                success = true,
                docID = doc.Id,
                title = doc.Title,
                content = doc.Content ?? string.Empty,
                revision = DocumentRepository.GetCurrentRevision(doc.Id),
                permission = perm,
                message = "Đã tham gia tài liệu"
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  REAL-TIME OPS
        // ═══════════════════════════════════════════════════════════
        private void HandleOpInsert(Packet packet)
        {
            ProcessRealtimeOp(packet, "insert");
        }

        private void HandleOpDelete(Packet packet)
        {
            ProcessRealtimeOp(packet, "delete");
        }

        private void ProcessRealtimeOp(Packet packet, string opType)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            // OP_INSERT và OP_DELETE dùng chung shape payload (cùng field)
            var payload = opType == "insert"
                ? (object)packet.GetPayload<Payload_OP_INSERT>()
                : packet.GetPayload<Payload_OP_DELETE>();

            string docId = null;
            int clientRevision = 0;
            List<EditOpItem> ops = null;

            if (opType == "insert")
            {
                var p = (Payload_OP_INSERT)payload;
                docId = p?.docID;
                clientRevision = p?.clientResivion ?? 0;
                ops = p?.ops;
            }
            else
            {
                var p = (Payload_OP_DELETE)payload;
                docId = p?.docID;
                clientRevision = p?.clientResivion ?? 0;
                ops = p?.ops;
            }

            if (string.IsNullOrWhiteSpace(docId)) { ReplyError(packet, "docID không hợp lệ."); return; }

            string perm = GetPermission(docId, currentUserId);
            if (perm != "owner" && perm != "editor")
            {
                ReplyError(packet, "Bạn không có quyền chỉnh sửa tài liệu này.");
                return;
            }

            int charCount = (ops ?? new List<EditOpItem>()).Sum(op => op?.text?.Length ?? 0);
            if (charCount > 5)
            {
                ReplyError(packet, $"OP_{opType.ToUpper()} chỉ được chứa tối đa 5 ký tự.");
                return;
            }

            // Reply OK cho người gửi
            ReplyOk(packet, $"OP_{opType.ToUpper()} received");

            // Broadcast cho mọi client khác trong room (trừ chính người gửi)
            var broadcast = new Payload_OP_BROADCAST
            {
                docID = docId,
                clientResivion = clientRevision,
                userID = currentUserId,
                username = _username,
                opType = opType,
                ops = ops ?? new List<EditOpItem>()
            };
            var pkt = Packet.Create(MessageType.OP_BROADCAST, broadcast);
            SessionManager.BroadcastToRoom(docId, pkt, exclude: this);
        }

        // ═══════════════════════════════════════════════════════════
        //  CHAT
        // ═══════════════════════════════════════════════════════════
        private void HandleChatSend(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_CHAT_SEND_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID) || string.IsNullOrWhiteSpace(p.content))
            {
                Reply(packet, MessageType.CHAT_SEND, new Payload_CHAT_SEND_Response
                { success = false, message = "Tin nhắn không hợp lệ." });
                return;
            }

            string perm = GetPermission(p.docID, currentUserId);
            if (perm == null)
            {
                Reply(packet, MessageType.CHAT_SEND, new Payload_CHAT_SEND_Response
                { success = false, message = "Bạn không có quyền trên tài liệu này." });
                return;
            }

            string content = p.content.Trim();
            if (content.Length > 2000) content = content.Substring(0, 2000);

            long id = ChatRepository.Create(new ChatMessage
            {
                DocId = p.docID,
                UserId = currentUserId,
                Content = content
            });

            var broadcast = new Payload_CHAT_BROADCAST
            {
                docID = p.docID,
                messageId = id,
                userId = currentUserId,
                username = _username,
                content = content,
                sentAt = DateTime.UtcNow
            };
            SessionManager.BroadcastToRoom(p.docID, Packet.Create(MessageType.CHAT_BROADCAST, broadcast));

            Reply(packet, MessageType.CHAT_SEND, new Payload_CHAT_SEND_Response
            { success = true, message = "Đã gửi" });
        }

        private void HandleChatHistory(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_CHAT_HISTORY_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID))
            { ReplyError(packet, "docID không hợp lệ."); return; }

            string perm = GetPermission(p.docID, currentUserId);
            if (perm == null) { ReplyError(packet, "Không có quyền truy cập."); return; }

            int limit = p.limit > 0 && p.limit <= 200 ? p.limit : 50;
            var rows = ChatRepository.GetRecent(p.docID, limit);

            var resp = new Payload_CHAT_HISTORY_Response
            {
                docID = p.docID,
                messages = rows.Select(r => new Payload_CHAT_BROADCAST
                {
                    docID = r.DocId,
                    messageId = r.Id,
                    userId = r.UserId,
                    username = r.Username,
                    content = r.Content,
                    sentAt = r.SentAt
                }).ToList()
            };
            Reply(packet, MessageType.CHAT_HISTORY, resp);
        }

        // ═══════════════════════════════════════════════════════════
        //  COMMENT
        // ═══════════════════════════════════════════════════════════
        private void HandleCommentCreate(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_COMMENT_CREATE_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID) || string.IsNullOrWhiteSpace(p.content))
            {
                Reply(packet, MessageType.COMMENT_CREATE, new Payload_COMMENT_CREATE_Response
                { success = false, message = "Comment không hợp lệ." });
                return;
            }

            string perm = GetPermission(p.docID, currentUserId);
            if (perm == null)
            {
                Reply(packet, MessageType.COMMENT_CREATE, new Payload_COMMENT_CREATE_Response
                { success = false, message = "Không có quyền." });
                return;
            }

            string id = CommentRepository.Create(new Comment
            {
                DocId = p.docID,
                UserId = currentUserId,
                ParentId = p.parentId,
                AnchorStart = p.anchorStart,
                AnchorEnd = p.anchorEnd,
                AnchorText = p.anchorText,
                Content = p.content
            });
            var saved = CommentRepository.GetById(id);
            var dto = ToDto(saved);

            Reply(packet, MessageType.COMMENT_CREATE, new Payload_COMMENT_CREATE_Response
            { success = true, comment = dto });

            SessionManager.BroadcastToRoom(p.docID,
                Packet.Create(MessageType.COMMENT_BROADCAST, new Payload_COMMENT_BROADCAST
                { action = "created", comment = dto }),
                exclude: this);
        }

        private void HandleCommentList(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_COMMENT_LIST_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID))
            { ReplyError(packet, "docID không hợp lệ."); return; }

            string perm = GetPermission(p.docID, currentUserId);
            if (perm == null) { ReplyError(packet, "Không có quyền."); return; }

            var rows = CommentRepository.GetByDoc(p.docID);
            Reply(packet, MessageType.COMMENT_LIST, new Payload_COMMENT_LIST_Response
            {
                docID = p.docID,
                comments = rows.Select(ToDto).ToList()
            });
        }

        private void HandleCommentResolve(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_COMMENT_RESOLVE_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.commentId))
            { ReplyError(packet, "commentId không hợp lệ."); return; }

            var c = CommentRepository.GetById(p.commentId);
            if (c == null) { ReplyError(packet, "Không tìm thấy comment."); return; }

            string perm = GetPermission(c.DocId, currentUserId);
            if (perm == null) { ReplyError(packet, "Không có quyền."); return; }

            CommentRepository.SetResolved(p.commentId, p.resolved);
            var updated = CommentRepository.GetById(p.commentId);
            var dto = ToDto(updated);

            ReplyOk(packet, "Đã cập nhật trạng thái comment");

            SessionManager.BroadcastToRoom(c.DocId,
                Packet.Create(MessageType.COMMENT_BROADCAST, new Payload_COMMENT_BROADCAST
                { action = "resolved", comment = dto }));
        }

        private void HandleCommentDelete(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_COMMENT_DELETE_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.commentId))
            { ReplyError(packet, "commentId không hợp lệ."); return; }

            var c = CommentRepository.GetById(p.commentId);
            if (c == null) { ReplyError(packet, "Không tìm thấy comment."); return; }

            // Cho phép tác giả comment hoặc owner doc xoá
            var doc = DocumentRepository.GetById(c.DocId);
            bool canDelete = c.UserId == currentUserId || (doc != null && doc.OwnerId == currentUserId);
            if (!canDelete) { ReplyError(packet, "Không có quyền xoá comment này."); return; }

            CommentRepository.Delete(p.commentId);
            ReplyOk(packet, "Đã xoá comment");

            SessionManager.BroadcastToRoom(c.DocId,
                Packet.Create(MessageType.COMMENT_BROADCAST, new Payload_COMMENT_BROADCAST
                { action = "deleted", comment = ToDto(c) }));
        }

        private CommentDto ToDto(CommentWithUser c)
        {
            if (c == null) return null;
            return new CommentDto
            {
                id = c.Id,
                docID = c.DocId,
                parentId = c.ParentId,
                userId = c.UserId,
                username = c.Username,
                anchorStart = c.AnchorStart,
                anchorEnd = c.AnchorEnd,
                anchorText = c.AnchorText,
                content = c.Content,
                resolved = c.Resolved,
                createdAt = c.CreatedAt
            };
        }

        // Overload cho Comment thường (không có Username)
        private CommentDto ToDto(Comment c)
        {
            if (c == null) return null;
            return new CommentDto
            {
                id = c.Id,
                docID = c.DocId,
                parentId = c.ParentId,
                userId = c.UserId,
                username = null,
                anchorStart = c.AnchorStart,
                anchorEnd = c.AnchorEnd,
                anchorText = c.AnchorText,
                content = c.Content,
                resolved = c.Resolved,
                createdAt = c.CreatedAt
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  IMAGE
        // ═══════════════════════════════════════════════════════════
        private void HandleImageUpload(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_IMAGE_UPLOAD_Request>();
            if (p == null || p.data == null || p.data.Length == 0)
            {
                Reply(packet, MessageType.IMAGE_UPLOAD, new Payload_IMAGE_UPLOAD_Response
                { success = false, message = "Dữ liệu ảnh rỗng." });
                return;
            }
            if (p.data.Length > ImageStorageService.MaxImageBytes)
            {
                Reply(packet, MessageType.IMAGE_UPLOAD, new Payload_IMAGE_UPLOAD_Response
                { success = false, message = $"Ảnh vượt quá {ImageStorageService.MaxImageBytes / (1024 * 1024)} MB." });
                return;
            }

            try
            {
                string url = ImageStorageService.Save(currentUserId, p.fileName, p.data, p.mimeType);
                string id = ImageRepository.Create(new Image
                {
                    UserId = currentUserId,
                    FileName = p.fileName,
                    MimeType = p.mimeType,
                    Url = url,
                    Size = p.data.Length
                });

                string avatarUrl = null;
                if (p.isAvatar)
                {
                    ImageRepository.UpdateUserAvatar(currentUserId, url);
                    avatarUrl = url;
                }

                Reply(packet, MessageType.IMAGE_UPLOAD, new Payload_IMAGE_UPLOAD_Response
                {
                    success = true,
                    imageId = id,
                    url = url,
                    avatarUrl = avatarUrl,
                    message = "Upload thành công"
                });
            }
            catch (Exception ex)
            {
                Reply(packet, MessageType.IMAGE_UPLOAD, new Payload_IMAGE_UPLOAD_Response
                { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        private void HandleImageGet(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_IMAGE_GET_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.imageId))
            {
                Reply(packet, MessageType.IMAGE_GET, new Payload_IMAGE_GET_Response
                { success = false, message = "imageId không hợp lệ." });
                return;
            }

            var img = ImageRepository.GetById(p.imageId);
            if (img == null)
            {
                Reply(packet, MessageType.IMAGE_GET, new Payload_IMAGE_GET_Response
                { success = false, message = "Không tìm thấy ảnh." });
                return;
            }

            byte[] bytes = ImageStorageService.Load(img.Url) ?? img.FileData;
            Reply(packet, MessageType.IMAGE_GET, new Payload_IMAGE_GET_Response
            {
                success = bytes != null && bytes.Length > 0,
                imageId = img.Id,
                mimeType = img.MimeType,
                data = bytes ?? new byte[0],
                message = bytes == null ? "File không tồn tại trên server" : "OK"
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  VERSION
        // ═══════════════════════════════════════════════════════════
        private void HandleVersionList(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_VERSION_LIST_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID))
            { ReplyError(packet, "docID không hợp lệ."); return; }

            string perm = GetPermission(p.docID, currentUserId);
            if (perm == null) { ReplyError(packet, "Không có quyền."); return; }

            int total = DocumentVersionRepository.CountVersions(p.docID);
            var rows = DocumentVersionRepository.GetVersions(p.docID, p.page, p.limit);

            var versions = new List<VersionInfoDto>();
            foreach (var r in rows)
            {
                versions.Add(new VersionInfoDto
                {
                    id = r.id,
                    savedAt = r.saved_at,
                    savedByUserId = r.saved_by,
                    savedByUsername = r.saved_by_username,
                    label = r.label
                });
            }

            Reply(packet, MessageType.DOC_VERSION_LIST, new Payload_DOC_VERSION_LIST_Response
            { docID = p.docID, total = total, versions = versions });
        }

        private void HandleVersionDetail(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_VERSION_DETAIL_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.versionId))
            { ReplyError(packet, "versionId không hợp lệ."); return; }

            var v = DocumentVersionRepository.GetVersionDetail(p.versionId);
            if (v == null)
            {
                Reply(packet, MessageType.DOC_VERSION_DETAIL, new Payload_DOC_VERSION_DETAIL_Response
                { success = false, message = "Không tìm thấy version." });
                return;
            }

            string perm = GetPermission(v.DocId, currentUserId);
            if (perm == null)
            {
                Reply(packet, MessageType.DOC_VERSION_DETAIL, new Payload_DOC_VERSION_DETAIL_Response
                { success = false, message = "Không có quyền." });
                return;
            }

            Reply(packet, MessageType.DOC_VERSION_DETAIL, new Payload_DOC_VERSION_DETAIL_Response
            {
                success = true,
                id = v.Id,
                docID = v.DocId,
                contentSnapshot = v.ContentSnapshot,
                savedAt = v.SavedAt,
                label = v.Label
            });
        }

        private void HandleVersionRestore(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_VERSION_RESTORE_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID) || string.IsNullOrWhiteSpace(p.versionId))
            { ReplyError(packet, "Tham số không hợp lệ."); return; }

            string perm = GetPermission(p.docID, currentUserId);
            if (perm != "owner" && perm != "editor")
            {
                Reply(packet, MessageType.DOC_VERSION_RESTORE, new Payload_DOC_VERSION_RESTORE_Response
                { success = false, message = "Bạn không có quyền khôi phục." });
                return;
            }

            bool ok = DocumentVersionRepository.RestoreVersion(p.docID, p.versionId);
            if (!ok)
            {
                Reply(packet, MessageType.DOC_VERSION_RESTORE, new Payload_DOC_VERSION_RESTORE_Response
                { success = false, message = "Không khôi phục được." });
                return;
            }

            // Lưu thêm 1 version sau restore (đánh dấu)
            try
            {
                var doc = DocumentRepository.GetById(p.docID);
                DocumentVersionRepository.SaveVersion(new DocumentVersion
                {
                    DocId = p.docID,
                    ContentSnapshot = doc?.Content ?? string.Empty,
                    SavedBy = currentUserId,
                    Label = $"Restored from {p.versionId}"
                });
            }
            catch { /* best-effort */ }

            var newDoc = DocumentRepository.GetById(p.docID);
            int rev = DocumentRepository.GetCurrentRevision(p.docID);

            Reply(packet, MessageType.DOC_VERSION_RESTORE, new Payload_DOC_VERSION_RESTORE_Response
            {
                success = true,
                message = "Khôi phục thành công",
                newContent = newDoc?.Content ?? string.Empty,
                revision = rev
            });

            // Notify mọi client trong room phải reload doc
            SessionManager.BroadcastToRoom(p.docID,
                Packet.Create(MessageType.DOC_RELOAD_BROADCAST, new Payload_DOC_RELOAD_BROADCAST
                { docID = p.docID, reason = "version_restored", revision = rev }));
        }

        private void HandleVersionDelete(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_VERSION_DELETE_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.versionId))
            { ReplyError(packet, "versionId không hợp lệ."); return; }

            var v = DocumentVersionRepository.GetVersionDetail(p.versionId);
            if (v == null)
            {
                Reply(packet, MessageType.DOC_VERSION_DELETE, new Payload_DOC_VERSION_DELETE_Response
                { success = false, message = "Không tìm thấy version." });
                return;
            }

            var doc = DocumentRepository.GetById(v.DocId);
            if (doc == null || doc.OwnerId != currentUserId)
            {
                Reply(packet, MessageType.DOC_VERSION_DELETE, new Payload_DOC_VERSION_DELETE_Response
                { success = false, message = "Chỉ chủ sở hữu mới được xoá version." });
                return;
            }

            bool ok = DocumentVersionRepository.DeleteVersion(p.versionId);
            Reply(packet, MessageType.DOC_VERSION_DELETE, new Payload_DOC_VERSION_DELETE_Response
            { success = ok, message = ok ? "Đã xoá" : "Không xoá được" });
        }

        // ═══════════════════════════════════════════════════════════
        //  AI
        // ═══════════════════════════════════════════════════════════
        private void HandleAiRequest(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            // Throttle
            var now = DateTime.UtcNow;
            if ((now - _lastAiRequestUtc).TotalSeconds < 3)
            {
                Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
                { success = false, message = "Vui lòng chờ vài giây trước khi hỏi tiếp." });
                return;
            }
            _lastAiRequestUtc = now;

            var p = packet.GetPayload<Payload_AI_Request>();
            if (p == null || (string.IsNullOrWhiteSpace(p.userPrompt) && string.IsNullOrWhiteSpace(p.contextText)))
            {
                Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
                { success = false, message = "Prompt rỗng." });
                return;
            }
            if ((p.userPrompt?.Length ?? 0) + (p.contextText?.Length ?? 0) > 20000)
            {
                Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
                { success = false, message = "Prompt quá dài." });
                return;
            }

            // Chạy bất đồng bộ để không chặn loop chính
            Packet originPacket = packet;
            Task.Run(async () =>
            {
                try
                {
                    string text = await AISuggestionService
                        .AskAsync(p.mode, p.userPrompt, p.contextText)
                        .ConfigureAwait(false);

                    Reply(originPacket, MessageType.AI_RESPONSE, new Payload_AI_Response
                    { success = true, text = text });
                }
                catch (Exception ex)
                {
                    Reply(originPacket, MessageType.AI_RESPONSE, new Payload_AI_Response
                    { success = false, message = ex.Message });
                }
            });
        }
    }
}