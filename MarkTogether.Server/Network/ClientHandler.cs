using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MarkTogether.Server.Database.Models;
using MarkTogether.Server.Database.Repositories;
using MarkTogether.Server.OT;
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
        private readonly Stream _stream;
        private readonly X509Certificate2 _serverCert;
        private string _token;
        private int _userId = -1;
        private string _username;

        // Throttle AI request: 1 req / 3 giây / user
        private DateTime _lastAiRequestUtc = DateTime.MinValue;

        public int UserId => _userId;
        public string Username => _username;
        public string Token => _token;

        public ClientHandler(TcpClient tcpClient, X509Certificate2 serverCert)
        {
            _tcpClient = tcpClient;
            _serverCert = serverCert;

            var ssl = new SslStream(tcpClient.GetStream(), false);
            ssl.AuthenticateAsServer(
                _serverCert,
                false,
                SslProtocols.Tls12,
                false);

            _stream = ssl;
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
                        case MessageType.AUTH_FORGOT_PASSWORD: HandleForgotPassword(packet); break;
                        case MessageType.AUTH_RESET_PASSWORD: HandleResetPassword(packet); break;

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
                        case MessageType.DOC_CREATE_LINK: HandleDocCreateLink(packet); break;
                        case MessageType.DOC_REVOKE_LINK: HandleDocRevokeLink(packet); break;
                        case MessageType.DOC_JOIN_LINK: HandleDocJoinLink(packet); break;
                        case MessageType.DOC_SET_VISIBILITY: HandleDocSetVisibility(packet); break;
                        case MessageType.DOC_GET_PUBLIC_LIST: HandleDocGetPublicList(packet); break;
                        case MessageType.DOC_SEARCH: HandleDocSearch(packet); break;
                        case MessageType.DOC_DELETE: HandleDocDelete(packet); break;

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
                if (!string.IsNullOrEmpty(_token) && SessionManager.RemoveSessionOnDisconnect)
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
        /// Permission được resolve tập trung qua RBAC service để tránh lệch logic giữa các handler.
        /// </summary>
        private string GetPermission(string docId, int userId)
        {
            return DocumentPermissionService.ResolvePermission(docId, userId);
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

        private void HandleForgotPassword(Packet packet)
        {
            var payload = packet.GetPayload<Payload_AUTH_FORGOT_PASSWORD_Request>();
            Packet origin = packet;
            Task.Run(async () =>
            {
                var response = await AuthService.RequestPasswordResetAsync(payload?.Email);
                Reply(origin, MessageType.AUTH_FORGOT_PASSWORD, response);
            });
        }

        private void HandleResetPassword(Packet packet)
        {
            var payload = packet.GetPayload<Payload_AUTH_RESET_PASSWORD_Request>();
            var response = AuthService.ResetPassword(payload?.Email, payload?.Otp, payload?.NewPassword);
            Reply(packet, MessageType.AUTH_RESET_PASSWORD, response);
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
                    visibility = DocumentPermissionService.NormalizeVisibility(d.Visibility),
                    publicPermission = DocumentPermissionService.NormalizePublicPermission(d.PublicPermission),
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
            if (!DocumentPermissionService.CanRead(permission)) { ReplyError(packet, "Bạn không có quyền truy cập tài liệu này."); return; }

            // Owner thấy share code, viewer/editor/commenter không cần
            string shareCode = DocumentPermissionService.IsOwner(permission) ? document.ShareCode : null;

            SessionManager.JoinRoom(docId, this);

            Reply(packet, MessageType.DOC_OPEN, new Payload_DOC_OPEN_Response
            {
                docID = document.Id,
                title = document.Title,
                content = document.Content ?? string.Empty,
                revision = DocumentRepository.GetCurrentRevision(docId),
                permission = permission,
                shareCode = shareCode,
                visibility = DocumentPermissionService.NormalizeVisibility(document.Visibility),
                publicPermission = DocumentPermissionService.NormalizePublicPermission(document.PublicPermission)
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
            if (!DocumentPermissionService.CanEdit(permission))
            {
                ReplyError(packet, "Bạn không có quyền lưu tài liệu này.");
                Console.WriteLine($"[RBAC] DOC_SAVE denied user={currentUserId} doc={docId} permission={permission ?? "none"}");
                return;
            }

            string newContent = payload.content ?? string.Empty;
            string kind = (payload.kind ?? "manual").Trim().ToLowerInvariant();
            if (kind != "draft" && kind != "periodic" && kind != "manual")
                kind = "manual";

            if (!DocumentRepository.UpdateContent(docId, newContent))
            {
                ReplyError(packet, "Lưu tài liệu thất bại.");
                return;
            }

            // CN9: phân loại version manual / periodic / draft bằng prefix label, không đổi schema.
            try
            {
                if (kind == "draft")
                {
                    DocumentVersionRepository.UpsertDraftVersion(
                        docId,
                        newContent,
                        currentUserId,
                        $"draft:by {_username}");
                }
                else
                {
                    string label = kind == "periodic"
                        ? $"periodic:{payload.periodicIntervalMin}m by {_username}"
                        : $"manual:Saved by {_username}";

                    DocumentVersionRepository.SaveVersion(new DocumentVersion
                    {
                        DocId = docId,
                        ContentSnapshot = newContent,
                        SavedBy = currentUserId,
                        Label = label
                    });
                    TrimVersions(docId, 50);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handler] Lưu version thất bại: {ex.Message}");
            }

            ReplyOk(packet, "Lưu tài liệu thành công");
        }

        private void HandleDocDelete(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var payload = packet.GetPayload<Payload_DOC_DELETE_Request>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID))
            {
                Reply(packet, MessageType.DOC_DELETE, new Payload_DOC_DELETE_Response
                { success = false, message = "docID không hợp lệ." });
                return;
            }

            string docId = payload.docID.Trim();
            string permission = GetPermission(docId, currentUserId);
            if (!DocumentPermissionService.IsOwner(permission))
            {
                Reply(packet, MessageType.DOC_DELETE, new Payload_DOC_DELETE_Response
                { success = false, docID = docId, message = "Chỉ chủ sở hữu mới được xóa tài liệu." });
                Console.WriteLine($"[RBAC] DOC_DELETE denied user={currentUserId} doc={docId} permission={permission ?? "none"}");
                return;
            }

            bool deleted = DocumentRepository.SoftDelete(docId, currentUserId);
            if (!deleted)
            {
                Reply(packet, MessageType.DOC_DELETE, new Payload_DOC_DELETE_Response
                { success = false, docID = docId, message = "Xóa tài liệu thất bại hoặc tài liệu đã bị xóa." });
                return;
            }

            SessionManager.BroadcastToRoom(docId, Packet.Create(MessageType.DOC_RELOAD_BROADCAST, new Payload_DOC_RELOAD_BROADCAST
            {
                docID = docId,
                reason = "deleted",
                revision = DocumentRepository.GetCurrentRevision(docId)
            }));

            Reply(packet, MessageType.DOC_DELETE, new Payload_DOC_DELETE_Response
            {
                success = true,
                docID = docId,
                message = "Đã xóa tài liệu."
            });
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

            string docId = p.docID.Trim();
            var doc = DocumentRepository.GetById(docId);
            string requesterPermission = GetPermission(docId, currentUserId);
            if (doc == null || !DocumentPermissionService.CanManage(requesterPermission))
            {
                Reply(packet, MessageType.DOC_SHARE, new Payload_DOC_SHARE_Response
                { success = false, message = "Chỉ chủ sở hữu mới được chia sẻ." });
                Console.WriteLine($"[RBAC] DOC_SHARE denied user={currentUserId} doc={docId} permission={requesterPermission ?? "none"}");
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

            string permission = DocumentPermissionService.NormalizeSharePermission(p.permission);

            try
            {
                // Nếu đã có share rồi → update permission
                if (DocumentShareRepository.GetPermission(docId, target.Id) != null)
                {
                    DocumentShareRepository.UpdatePermission(docId, target.Id, permission);
                }
                else
                {
                    DocumentShareRepository.ShareDocument(docId, target.Id, permission);
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

            string docId = p.docID.Trim();
            var doc = DocumentRepository.GetById(docId);
            if (doc == null) { ReplyError(packet, "Không tìm thấy tài liệu."); return; }

            string requesterPermission = GetPermission(docId, currentUserId);
            if (!DocumentPermissionService.CanManage(requesterPermission))
            {
                Reply(packet, MessageType.DOC_SHARE_LIST, new Payload_DOC_SHARE_LIST_Response
                { success = false, message = "Chỉ chủ sở hữu mới xem được danh sách." });
                Console.WriteLine($"[RBAC] DOC_SHARE_LIST denied user={currentUserId} doc={docId} permission={requesterPermission ?? "none"}");
                return;
            }

            var rows = DocumentShareRepository.GetCollaborators(docId);
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
                docID = docId,
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

            if (string.IsNullOrWhiteSpace(p.docID) || p.targetUserId <= 0)
            {
                Reply(packet, MessageType.DOC_SHARE_UPDATE, new Payload_DOC_SHARE_UPDATE_Response
                { success = false, message = "Tham số không hợp lệ." });
                return;
            }

            string docId = p.docID.Trim();
            var doc = DocumentRepository.GetById(docId);
            string requesterPermission = GetPermission(docId, currentUserId);
            if (doc == null || !DocumentPermissionService.CanManage(requesterPermission))
            {
                Reply(packet, MessageType.DOC_SHARE_UPDATE, new Payload_DOC_SHARE_UPDATE_Response
                { success = false, message = "Không có quyền." });
                Console.WriteLine($"[RBAC] DOC_SHARE_UPDATE denied user={currentUserId} doc={docId} permission={requesterPermission ?? "none"}");
                return;
            }

            string perm = DocumentPermissionService.NormalizeSharePermission(p.newPermission);

            bool ok = DocumentShareRepository.UpdatePermission(docId, p.targetUserId, perm);
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

            if (string.IsNullOrWhiteSpace(p.docID) || p.targetUserId <= 0)
            {
                Reply(packet, MessageType.DOC_SHARE_REVOKE, new Payload_DOC_SHARE_REVOKE_Response
                { success = false, message = "Tham số không hợp lệ." });
                return;
            }

            string docId = p.docID.Trim();
            var doc = DocumentRepository.GetById(docId);
            string requesterPermission = GetPermission(docId, currentUserId);
            if (doc == null || !DocumentPermissionService.CanManage(requesterPermission))
            {
                Reply(packet, MessageType.DOC_SHARE_REVOKE, new Payload_DOC_SHARE_REVOKE_Response
                { success = false, message = "Không có quyền." });
                Console.WriteLine($"[RBAC] DOC_SHARE_REVOKE denied user={currentUserId} doc={docId} permission={requesterPermission ?? "none"}");
                return;
            }

            bool ok = DocumentShareRepository.RevokeAccess(docId, p.targetUserId);
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

            string docId = p.docID.Trim();
            var doc = DocumentRepository.GetById(docId);
            string requesterPermission = GetPermission(docId, currentUserId);
            if (doc == null || !DocumentPermissionService.CanManage(requesterPermission))
            {
                Reply(packet, MessageType.DOC_SHARE_REGEN_CODE, new Payload_DOC_SHARE_REGEN_CODE_Response
                { success = false, message = "Không có quyền." });
                Console.WriteLine($"[RBAC] DOC_SHARE_REGEN_CODE denied user={currentUserId} doc={docId} permission={requesterPermission ?? "none"}");
                return;
            }

            string newCode = ShareCodeGenerator.GenerateUnique();
            DocumentRepository.UpdateShareCode(docId, newCode);

            Reply(packet, MessageType.DOC_SHARE_REGEN_CODE, new Payload_DOC_SHARE_REGEN_CODE_Response
            {
                success = true,
                shareCode = newCode,
                message = "Đã tạo mã chia sẻ mới"
            });
        }

        private void HandleDocCreateLink(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_CREATE_LINK_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID))
            {
                Reply(packet, MessageType.DOC_CREATE_LINK, new Payload_DOC_CREATE_LINK_Response
                { success = false, message = "docID không hợp lệ." });
                return;
            }

            string docId = p.docID.Trim();
            string requesterPermission = GetPermission(docId, currentUserId);
            if (!DocumentPermissionService.CanManage(requesterPermission))
            {
                Reply(packet, MessageType.DOC_CREATE_LINK, new Payload_DOC_CREATE_LINK_Response
                { success = false, message = "Chỉ chủ sở hữu mới được tạo sharing link." });
                Console.WriteLine($"[RBAC] DOC_CREATE_LINK denied user={currentUserId} doc={docId} permission={requesterPermission ?? "none"}");
                return;
            }

            string linkPermission = DocumentPermissionService.NormalizeSharePermission(p.permission);
            DateTime? expiresAt = null;
            if (p.expiresInHours > 0)
            {
                int safeHours = p.expiresInHours > 24 * 30 ? 24 * 30 : p.expiresInHours;
                expiresAt = DateTime.UtcNow.AddHours(safeHours);
            }

            int? maxUses = null;
            if (p.maxUses > 0)
                maxUses = p.maxUses > 1000 ? 1000 : p.maxUses;

            try
            {
                SharingLink link = null;
                for (int i = 0; i < 3 && link == null; i++)
                {
                    string token = SecureTokenGenerator.GenerateUrlSafeToken(48);
                    try
                    {
                        link = SharingLinkRepository.Create(docId, currentUserId, token, linkPermission, expiresAt, maxUses);
                    }
                    catch
                    {
                        if (i == 2) throw;
                    }
                }

                Console.WriteLine($"[ShareLink] created user={currentUserId} doc={docId} permission={linkPermission} expiresAt={expiresAt}");

                Reply(packet, MessageType.DOC_CREATE_LINK, new Payload_DOC_CREATE_LINK_Response
                {
                    success = true,
                    message = "Tạo sharing link thành công",
                    linkId = link?.Id,
                    linkToken = link?.Token,
                    permission = linkPermission,
                    expiresAt = expiresAt,
                    maxUses = maxUses
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShareLink] create failed user={currentUserId} doc={docId}: {ex.Message}");
                Reply(packet, MessageType.DOC_CREATE_LINK, new Payload_DOC_CREATE_LINK_Response
                { success = false, message = "Không tạo được sharing link." });
            }
        }

        private void HandleDocRevokeLink(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_REVOKE_LINK_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.linkToken))
            {
                Reply(packet, MessageType.DOC_REVOKE_LINK, new Payload_DOC_REVOKE_LINK_Response
                { success = false, message = "linkToken không hợp lệ." });
                return;
            }

            string token = p.linkToken.Trim();
            bool ok = SharingLinkRepository.RevokeByToken(token, currentUserId);
            Console.WriteLine($"[ShareLink] revoke user={currentUserId} token={token} success={ok}");
            Reply(packet, MessageType.DOC_REVOKE_LINK, new Payload_DOC_REVOKE_LINK_Response
            {
                success = ok,
                message = ok ? "Đã thu hồi sharing link" : "Không có quyền hoặc link không tồn tại"
            });
        }

        private void HandleDocJoinLink(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_JOIN_LINK_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.linkToken))
            {
                Reply(packet, MessageType.DOC_JOIN_LINK, new Payload_DOC_JOIN_LINK_Response
                { success = false, message = "linkToken không hợp lệ." });
                return;
            }

            string token = p.linkToken.Trim();
            var link = SharingLinkRepository.GetByToken(token);
            if (link == null || !link.IsActive)
            {
                Reply(packet, MessageType.DOC_JOIN_LINK, new Payload_DOC_JOIN_LINK_Response
                { success = false, message = "Sharing link không tồn tại hoặc đã bị thu hồi." });
                return;
            }

            if (link.ExpiresAt.HasValue && link.ExpiresAt.Value <= DateTime.UtcNow)
            {
                Reply(packet, MessageType.DOC_JOIN_LINK, new Payload_DOC_JOIN_LINK_Response
                { success = false, message = "Sharing link đã hết hạn." });
                return;
            }

            if (link.MaxUses.HasValue && link.UseCount >= link.MaxUses.Value)
            {
                Reply(packet, MessageType.DOC_JOIN_LINK, new Payload_DOC_JOIN_LINK_Response
                { success = false, message = "Sharing link đã vượt quá số lần sử dụng." });
                return;
            }

            var doc = DocumentRepository.GetById(link.DocId);
            if (doc == null || doc.DeletedAt.HasValue)
            {
                Reply(packet, MessageType.DOC_JOIN_LINK, new Payload_DOC_JOIN_LINK_Response
                { success = false, message = "Tài liệu không tồn tại." });
                return;
            }

            string permission = DocumentPermissionService.NormalizeSharePermission(link.Permission);
            string existingPermission = GetPermission(link.DocId, currentUserId);
            if (!DocumentPermissionService.IsOwner(existingPermission))
            {
                if (DocumentShareRepository.GetPermission(link.DocId, currentUserId) != null)
                    DocumentShareRepository.UpdatePermission(link.DocId, currentUserId, permission);
                else
                    DocumentShareRepository.ShareDocument(link.DocId, currentUserId, permission);
            }

            if (!SharingLinkRepository.IncrementUseCount(token))
            {
                Reply(packet, MessageType.DOC_JOIN_LINK, new Payload_DOC_JOIN_LINK_Response
                { success = false, message = "Sharing link không còn khả dụng." });
                return;
            }

            Console.WriteLine($"[ShareLink] joined user={currentUserId} doc={link.DocId} permission={permission}");

            Reply(packet, MessageType.DOC_JOIN_LINK, new Payload_DOC_JOIN_LINK_Response
            {
                success = true,
                message = "Đã tham gia tài liệu bằng sharing link",
                docID = doc.Id,
                title = doc.Title,
                content = doc.Content ?? string.Empty,
                revision = DocumentRepository.GetCurrentRevision(doc.Id),
                permission = DocumentPermissionService.IsOwner(existingPermission) ? existingPermission : permission
            });
        }

        private void HandleDocSetVisibility(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_SET_VISIBILITY_Request>();
            if (p == null || string.IsNullOrWhiteSpace(p.docID))
            {
                Reply(packet, MessageType.DOC_SET_VISIBILITY, new Payload_DOC_SET_VISIBILITY_Response
                { success = false, message = "docID không hợp lệ." });
                return;
            }

            string docId = p.docID.Trim();
            string visibility = DocumentPermissionService.NormalizeVisibility(p.visibility);
            string publicPermission = DocumentPermissionService.NormalizePublicPermission(p.publicPermission);

            var doc = DocumentRepository.GetById(docId);
            string requesterPermission = GetPermission(docId, currentUserId);
            if (doc == null || !DocumentPermissionService.CanManage(requesterPermission))
            {
                Reply(packet, MessageType.DOC_SET_VISIBILITY, new Payload_DOC_SET_VISIBILITY_Response
                { success = false, message = "Chỉ chủ sở hữu mới được thay đổi visibility.", docID = docId });
                Console.WriteLine($"[RBAC] DOC_SET_VISIBILITY denied user={currentUserId} doc={docId} permission={requesterPermission ?? "none"}");
                return;
            }

            bool ok = DocumentRepository.UpdateVisibility(docId, visibility, publicPermission);
            if (!ok)
            {
                Reply(packet, MessageType.DOC_SET_VISIBILITY, new Payload_DOC_SET_VISIBILITY_Response
                { success = false, message = "Cập nhật visibility thất bại.", docID = docId });
                return;
            }

            Console.WriteLine($"[Visibility] user={currentUserId} doc={docId} visibility={visibility} publicPermission={publicPermission}");

            Reply(packet, MessageType.DOC_SET_VISIBILITY, new Payload_DOC_SET_VISIBILITY_Response
            {
                success = true,
                message = "Cập nhật visibility thành công",
                docID = docId,
                visibility = visibility,
                publicPermission = publicPermission
            });
        }

        private void HandleDocGetPublicList(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_GET_PUBLIC_LIST_Request>() ?? new Payload_DOC_GET_PUBLIC_LIST_Request();
            int page = p.page < 1 ? 1 : p.page;
            int limit = p.limit < 1 ? 20 : (p.limit > 100 ? 100 : p.limit);

            int total = DocumentRepository.CountPublicDocuments();
            var rows = DocumentRepository.GetPublicDocuments(page, limit);

            Reply(packet, MessageType.DOC_GET_PUBLIC_LIST, new Payload_DOC_GET_PUBLIC_LIST_Response
            {
                success = true,
                message = "OK",
                page = page,
                limit = limit,
                total = total,
                documents = rows.Select(r => new PublicDocumentDto
                {
                    docID = r.Id,
                    title = r.Title,
                    ownerId = r.OwnerId,
                    ownerUsername = r.OwnerUsername,
                    updatedAt = r.UpdatedAt,
                    publicPermission = DocumentPermissionService.NormalizePublicPermission(r.PublicPermission)
                }).ToList()
            });
        }

        private void HandleDocSearch(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { ReplyError(packet, "Phiên đăng nhập không hợp lệ."); return; }

            var p = packet.GetPayload<Payload_DOC_SEARCH_Request>();
            string query = (p?.query ?? string.Empty).Trim();
            string searchBy = (p?.searchBy ?? "all").Trim().ToLowerInvariant();

            if (query.Length < 2)
            {
                Reply(packet, MessageType.DOC_SEARCH, new Payload_DOC_SEARCH_Response
                { success = false, message = "Từ khóa tìm kiếm phải có ít nhất 2 ký tự." });
                return;
            }

            if (searchBy != "id" && searchBy != "title" && searchBy != "all")
                searchBy = "all";

            try
            {
                var rows = DocumentRepository.SearchAccessibleDocuments(currentUserId, query, searchBy, 20);
                Reply(packet, MessageType.DOC_SEARCH, new Payload_DOC_SEARCH_Response
                {
                    success = true,
                    message = "OK",
                    results = rows.Select(r => new DocumentSearchResultDto
                    {
                        docID = r.Id,
                        title = r.Title,
                        ownerUsername = r.OwnerUsername,
                        visibility = DocumentPermissionService.NormalizeVisibility(r.Visibility),
                        permission = r.Permission,
                        updatedAt = r.UpdatedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Search] DOC_SEARCH failed user={currentUserId}: {ex.Message}");
                Reply(packet, MessageType.DOC_SEARCH, new Payload_DOC_SEARCH_Response
                { success = false, message = "Tìm kiếm thất bại." });
            }
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
            if (!DocumentPermissionService.CanEdit(perm))
            {
                ReplyError(packet, "Bạn không có quyền chỉnh sửa tài liệu này.");
                Console.WriteLine($"[RBAC] OP_{opType.ToUpper()} denied user={currentUserId} doc={docId} permission={perm ?? "none"}");
                return;
            }

            int charCount = (ops ?? new List<EditOpItem>()).Sum(op => op?.text?.Length ?? 0);
            if (charCount > 5)
            {
                ReplyError(packet, $"OP_{opType.ToUpper()} chỉ được chứa tối đa 5 ký tự.");
                return;
            }

            // [OT] Lấy/tạo state cho document này
            var state = DocumentStateManager.GetOrCreate(docId);

            var transformedOps = new List<EditOpItem>();
            foreach (var op in ops ?? new List<EditOpItem>())
            {
                Console.WriteLine($"[OT] {opType.ToUpper()} user={currentUserId} " +
                    $"clientRev={clientRevision} serverRev={state.ServerRevision} " +
                    $"pos={op.pos} text='{op.text?.Replace("\n", "\\n").Replace("\r", "\\r")}'" );

                // [OT] Transform op against concurrent server ops, persist vào DB
                var transformedOp = state.TransformAndApply(op, opType, clientRevision, currentUserId);
                transformedOps.Add(transformedOp);

                Console.WriteLine($"[OT] {opType.ToUpper()} transformed pos={transformedOp.pos} " +
                    $"newServerRev={state.ServerRevision}");
            }

            // Gửi ACK về cho client kèm serverRevision mới nhất
            Reply(packet, MessageType.OK, new Payload_OK { Message = state.ServerRevision.ToString() });

            // Broadcast từng op đã transform cho các client khác trong room
            foreach (var tOp in transformedOps)
            {
                var broadcast = new Payload_OP_BROADCAST
                {
                    docID = docId,
                    clientResivion = state.ServerRevision,
                    userID = currentUserId,
                    username = _username,
                    opType = opType,
                    ops = new List<EditOpItem> { tOp }
                };
                SessionManager.BroadcastToRoom(docId, Packet.Create(MessageType.OP_BROADCAST, broadcast), exclude: this);
            }
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

            string docId = p.docID.Trim();
            string perm = GetPermission(docId, currentUserId);
            if (!DocumentPermissionService.CanRead(perm))
            {
                Reply(packet, MessageType.CHAT_SEND, new Payload_CHAT_SEND_Response
                { success = false, message = "Bạn không có quyền trên tài liệu này." });
                Console.WriteLine($"[RBAC] CHAT_SEND denied user={currentUserId} doc={docId} permission={perm ?? "none"}");
                return;
            }

            string content = p.content.Trim();
            if (content.Length > 2000) content = content.Substring(0, 2000);

            long id = ChatRepository.Create(new ChatMessage
            {
                DocId = docId,
                UserId = currentUserId,
                Content = content
            });

            var broadcast = new Payload_CHAT_BROADCAST
            {
                docID = docId,
                messageId = id,
                userId = currentUserId,
                username = _username,
                content = content,
                sentAt = DateTime.UtcNow
            };
            SessionManager.BroadcastToRoom(docId, Packet.Create(MessageType.CHAT_BROADCAST, broadcast));

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

            string docId = p.docID.Trim();
            string perm = GetPermission(docId, currentUserId);
            if (!DocumentPermissionService.CanRead(perm)) { ReplyError(packet, "Không có quyền truy cập."); return; }

            int limit = p.limit > 0 && p.limit <= 200 ? p.limit : 50;
            var rows = ChatRepository.GetRecent(docId, limit);

            var resp = new Payload_CHAT_HISTORY_Response
            {
                docID = docId,
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

            string docId = p.docID.Trim();
            string perm = GetPermission(docId, currentUserId);
            if (!DocumentPermissionService.CanComment(perm))
            {
                Reply(packet, MessageType.COMMENT_CREATE, new Payload_COMMENT_CREATE_Response
                { success = false, message = "Bạn không có quyền bình luận trên tài liệu này." });
                Console.WriteLine($"[RBAC] COMMENT_CREATE denied user={currentUserId} doc={docId} permission={perm ?? "none"}");
                return;
            }

            string id = CommentRepository.Create(new Comment
            {
                DocId = docId,
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

            SessionManager.BroadcastToRoom(docId,
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

            string docId = p.docID.Trim();
            string perm = GetPermission(docId, currentUserId);
            if (!DocumentPermissionService.CanRead(perm)) { ReplyError(packet, "Không có quyền."); return; }

            var rows = CommentRepository.GetByDoc(docId);
            Reply(packet, MessageType.COMMENT_LIST, new Payload_COMMENT_LIST_Response
            {
                docID = docId,
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
            if (!DocumentPermissionService.CanComment(perm))
            {
                ReplyError(packet, "Bạn không có quyền cập nhật trạng thái comment.");
                Console.WriteLine($"[RBAC] COMMENT_RESOLVE denied user={currentUserId} doc={c.DocId} permission={perm ?? "none"}");
                return;
            }

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
            string perm = GetPermission(c.DocId, currentUserId);
            bool canDelete = c.UserId == currentUserId || DocumentPermissionService.IsOwner(perm);
            if (!canDelete)
            {
                ReplyError(packet, "Không có quyền xoá comment này.");
                Console.WriteLine($"[RBAC] COMMENT_DELETE denied user={currentUserId} doc={c.DocId} permission={perm ?? "none"} commentOwner={c.UserId}");
                return;
            }

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
                string docId = string.IsNullOrWhiteSpace(p.docID) ? null : p.docID.Trim();
                if (!string.IsNullOrWhiteSpace(docId))
                {
                    string permission = GetPermission(docId, currentUserId);
                    if (!DocumentPermissionService.CanEdit(permission))
                    {
                        Reply(packet, MessageType.IMAGE_UPLOAD, new Payload_IMAGE_UPLOAD_Response
                        { success = false, message = "Bạn không có quyền upload ảnh vào tài liệu này." });
                        Console.WriteLine($"[RBAC] IMAGE_UPLOAD denied user={currentUserId} doc={docId} permission={permission ?? "none"}");
                        return;
                    }
                }

                Image img = ImageStorageService.SaveWithDedup(currentUserId, p.fileName, p.data, p.mimeType, docId);

                string avatarUrl = null;
                if (p.isAvatar)
                {
                    ImageRepository.UpdateUserAvatar(currentUserId, img.Url);
                    avatarUrl = img.Url;
                }

                Reply(packet, MessageType.IMAGE_UPLOAD, new Payload_IMAGE_UPLOAD_Response
                {
                    success = true,
                    imageId = img.Id,
                    url = img.Url,
                    avatarUrl = avatarUrl,
                    message = img.UserId == currentUserId ? "Upload thành công" : "Ảnh đã tồn tại, tái sử dụng bản đã lưu"
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

            if (!ImageRepository.CanUserAccessImage(currentUserId, p.imageId))
            {
                Reply(packet, MessageType.IMAGE_GET, new Payload_IMAGE_GET_Response
                { success = false, message = "Bạn không có quyền truy cập ảnh này." });
                Console.WriteLine($"[RBAC] IMAGE_GET denied user={currentUserId} image={p.imageId}");
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

            string docId = p.docID.Trim();
            string perm = GetPermission(docId, currentUserId);
            if (!DocumentPermissionService.CanRead(perm)) { ReplyError(packet, "Không có quyền."); return; }

            int total = DocumentVersionRepository.CountVersions(docId);
            var rows = DocumentVersionRepository.GetVersions(docId, p.page, p.limit);

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
            { docID = docId, total = total, versions = versions });
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
            if (!DocumentPermissionService.CanRead(perm))
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

            string docId = p.docID.Trim();
            string perm = GetPermission(docId, currentUserId);
            if (!DocumentPermissionService.CanEdit(perm))
            {
                Reply(packet, MessageType.DOC_VERSION_RESTORE, new Payload_DOC_VERSION_RESTORE_Response
                { success = false, message = "Bạn không có quyền khôi phục." });
                Console.WriteLine($"[RBAC] DOC_VERSION_RESTORE denied user={currentUserId} doc={docId} permission={perm ?? "none"}");
                return;
            }

            bool ok = DocumentVersionRepository.RestoreVersion(docId, p.versionId);
            if (!ok)
            {
                Reply(packet, MessageType.DOC_VERSION_RESTORE, new Payload_DOC_VERSION_RESTORE_Response
                { success = false, message = "Không khôi phục được." });
                return;
            }

            // Lưu thêm 1 version sau restore (đánh dấu)
            try
            {
                var doc = DocumentRepository.GetById(docId);
                DocumentVersionRepository.SaveVersion(new DocumentVersion
                {
                    DocId = docId,
                    ContentSnapshot = doc?.Content ?? string.Empty,
                    SavedBy = currentUserId,
                    Label = $"Restored from {p.versionId}"
                });
            }
            catch { /* best-effort */ }

            var newDoc = DocumentRepository.GetById(docId);
            int rev = DocumentRepository.GetCurrentRevision(docId);

            Reply(packet, MessageType.DOC_VERSION_RESTORE, new Payload_DOC_VERSION_RESTORE_Response
            {
                success = true,
                message = "Khôi phục thành công",
                newContent = newDoc?.Content ?? string.Empty,
                revision = rev
            });

            // Notify mọi client trong room phải reload doc
            SessionManager.BroadcastToRoom(docId,
                Packet.Create(MessageType.DOC_RELOAD_BROADCAST, new Payload_DOC_RELOAD_BROADCAST
                { docID = docId, reason = "version_restored", revision = rev }));
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

            string perm = GetPermission(v.DocId, currentUserId);
            if (!DocumentPermissionService.CanManage(perm))
            {
                Reply(packet, MessageType.DOC_VERSION_DELETE, new Payload_DOC_VERSION_DELETE_Response
                { success = false, message = "Chỉ chủ sở hữu mới được xoá version." });
                Console.WriteLine($"[RBAC] DOC_VERSION_DELETE denied user={currentUserId} doc={v.DocId} permission={perm ?? "none"}");
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
            if (p == null)
            {
                Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
                { success = false, message = "Payload rỗng." });
                return;
            }

            string actionMode = string.IsNullOrWhiteSpace(p.actionMode)
                ? "text"
                : p.actionMode.Trim().ToLowerInvariant();

            int maxLen = actionMode == "edit" ? 80000 : 20000;
            int totalLen = (p.userPrompt?.Length ?? 0)
                + (p.contextText?.Length ?? 0)
                + (p.documentText?.Length ?? 0);
            if (totalLen > maxLen)
            {
                Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
                { success = false, message = $"Prompt quá dài (>{maxLen} ký tự)." });
                return;
            }

            if (actionMode == "text" &&
                string.IsNullOrWhiteSpace(p.userPrompt) && string.IsNullOrWhiteSpace(p.contextText))
            {
                Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
                { success = false, message = "Prompt rỗng." });
                return;
            }

            if (actionMode == "edit" && string.IsNullOrWhiteSpace(p.userPrompt))
            {
                Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
                { success = false, message = "Cần mô tả thao tác cần thực hiện." });
                return;
            }

            string keyHash = string.IsNullOrEmpty(p.apiKey)
                ? "(server-fallback)"
                : Sha256Short(p.apiKey);
            Console.WriteLine($"[AI] user={currentUserId} provider={p.provider ?? "gemini"} " +
                              $"model={p.model ?? "default"} keyHash={keyHash} actionMode={actionMode} " +
                              $"docLen={p.documentText?.Length ?? 0}");

            // Chạy bất đồng bộ để không chặn loop chính
            Packet originPacket = packet;
            Task.Run(async () =>
            {
                try
                {
                    if (actionMode == "edit")
                    {
                        string rawJson = await AISuggestionService.AskEditAsync(
                                p.provider, p.model, p.apiKey,
                                p.userPrompt,
                                p.documentText ?? "", p.documentText?.Length ?? 0,
                                p.selectionStart, p.selectionEnd, p.cursorPosition)
                            .ConfigureAwait(false);

                        var vr = AIEditPlanValidator.Validate(
                            rawJson, p.documentText?.Length ?? 0, p.selectionStart, p.selectionEnd);

                        if (!vr.Ok)
                        {
                            Reply(originPacket, MessageType.AI_RESPONSE, new Payload_AI_Response
                            {
                                success = true,
                                kind = "text",
                                text = "AI trả về kế hoạch không hợp lệ: " + vr.Reason +
                                       ". Bạn có thể thử lại hoặc diễn đạt rõ hơn."
                            });
                            return;
                        }

                        Reply(originPacket, MessageType.AI_RESPONSE, new Payload_AI_Response
                        {
                            success = true,
                            kind = vr.Kind,
                            text = vr.TextFallback ?? "",
                            editPlanJson = vr.NormalizedJson ?? ""
                        });
                    }
                    else
                    {
                        string text = await AISuggestionService
                            .AskAsync(p.provider, p.model, p.apiKey, p.mode, p.userPrompt, p.contextText)
                            .ConfigureAwait(false);

                        Reply(originPacket, MessageType.AI_RESPONSE, new Payload_AI_Response
                        { success = true, kind = "text", text = text });
                    }
                }
                catch (Exception ex)
                {
                    Reply(originPacket, MessageType.AI_RESPONSE, new Payload_AI_Response
                    { success = false, message = ex.Message });
                }
            });
        }

        private static string Sha256Short(string value)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value ?? ""));
                return BitConverter.ToString(hash, 0, 4).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
