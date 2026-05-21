using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using MarkTogether.Server.Database.Models;
using MarkTogether.Server.Database.Repositories;
using MarkTogether.Server.Services;
using MarkTogether.Shared;
using MarkTogether.Server.OT; // [OT]

namespace MarkTogether.Server.Network
{
    /// <summary>
    /// Xử lý từng client kết nối TCP.
    /// Mỗi client có 1 instance ClientHandler riêng chạy trên thread riêng.
    /// Đọc Packet → dispatch theo MessageType → trả kết quả.
    /// </summary>
    public class ClientHandler
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private string _token;      // Token của client này sau khi login
        private int _userId = -1;   // UserId sau khi xác thực
        private string _username;   // Username sau khi xác thực

        // [ADDED] Track current document for broadcasting
        private string _currentDocId;

        public int UserId => _userId;
        public string Username => _username;
        public string CurrentDocId => _currentDocId;

        public ClientHandler(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
        }

        // [ADDED] Expose method to send packet
        public void SendPacket(Packet packet)
        {
            PacketHelper.Send(_stream, packet);
        }

        /// <summary>
        /// Vòng lặp chính: đọc packet → xử lý → trả kết quả.
        /// Chạy cho đến khi client ngắt kết nối.
        /// </summary>
        public void ProcessAsync()
        {
            try
            {
                while (_tcpClient.Connected)
                {
                    // 1. Đọc packet từ client
                    Packet packet = PacketHelper.Receive(_stream);
                    if (packet == null) continue;

                    try
                    {
                        // 2. Dispatch theo MessageType
                        switch (packet.Type)
                        {
                            case MessageType.AUTH_REGISTER: HandleRegister(packet); break;
                            case MessageType.AUTH_LOGIN: HandleLogin(packet); break;
                            case MessageType.DOC_LIST: HandleDocList(packet); break;
                            case MessageType.DOC_CREATE: HandleDocCreate(packet); break;
                            case MessageType.DOC_OPEN: HandleDocOpen(packet); break;
                            case MessageType.DOC_SAVE: HandleDocSave(packet); break;
                            case MessageType.DOC_LEAVE: HandleDocLeave(packet); break;
                            case MessageType.DOC_SHARE: HandleDocShare(packet); break;
                            case MessageType.DOC_JOIN_CODE: HandleDocJoinCode(packet); break;
                            case MessageType.OP_INSERT: HandleOpInsert(packet); break;
                            case MessageType.OP_DELETE: HandleOpDelete(packet); break;
                            // [ADDED] Share management
                            case MessageType.DOC_GET_SHARES: HandleDocGetShares(packet); break;
                            case MessageType.DOC_REVOKE:     HandleDocRevoke(packet); break;
                            case MessageType.DOC_UPDATE_PERM: HandleDocUpdatePerm(packet); break;
                            case MessageType.DOC_SET_PUBLIC:  HandleDocSetPublic(packet); break;
                            default:
                                // Nếu chưa login → từ chối
                                if (_userId < 0)
                                {
                                    SendError("Bạn chưa đăng nhập. Vui lòng login trước.");
                                    break;
                                }
                                SendError($"MessageType '{packet.Type}' chưa được hỗ trợ.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Handler] Lỗi xử lý packet {packet?.Type}: {ex}");
                        SendError("Đã xảy ra lỗi khi xử lý yêu cầu.");
                    }
                }
            }
            catch (IOException)
            {
                // Client ngắt kết nối bình thường
                Console.WriteLine($"[Handler] Client {_username ?? "unknown"} (ID={_userId}) đã ngắt kết nối.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handler] Lỗi: {ex}");
            }
            finally
            {
                // Dọn dẹp session
                if (!string.IsNullOrEmpty(_token))
                    SessionManager.RemoveSession(_token);

                _stream?.Close();
                _tcpClient?.Close();

                Console.WriteLine($"[Handler] Đã dọn dẹp kết nối cho user {_username ?? "unknown"}.");
            }
        }

        // ═══════════════════════════════════════════
        //  XỬ LÝ ĐĂNG KÝ
        // ═══════════════════════════════════════════
        private void HandleRegister(Packet packet)
        {
            Console.WriteLine("[Handler] Nhận yêu cầu ĐĂNG KÝ...");
            var payload = packet.GetPayload<Payload_AUTH_REGISTER>();
            Payload_AUTH_RESPONSE response = AuthService.Register(payload.Username, payload.Email, payload.Password);
            if (response.Success)
            {
                _token = response.Token;
                _userId = response.UserId;
                _username = response.Username;
                SessionManager.AddSession(_token, _userId);
            }
            var responsePacket = Packet.Create(MessageType.AUTH_RESPONSE, response);
            PacketHelper.Send(_stream, responsePacket);
        }

        // ═══════════════════════════════════════════
        //  XỬ LÝ ĐĂNG NHẬP
        // ═══════════════════════════════════════════
        private void HandleLogin(Packet packet)
        {
            Console.WriteLine("[Handler] Nhận yêu cầu ĐĂNG NHẬP...");
            var payload = packet.GetPayload<Payload_AUTH_LOGIN>();
            Payload_AUTH_RESPONSE response = AuthService.Login(payload.Username, payload.Password);
            if (response.Success)
            {
                _token = response.Token;
                _userId = response.UserId;
                _username = response.Username;
                SessionManager.AddSession(_token, _userId);
            }
            var responsePacket = Packet.Create(MessageType.AUTH_RESPONSE, response);
            PacketHelper.Send(_stream, responsePacket);
        }

        private void HandleDocList(Packet packet)
        {
            try
            {
                int currentUserId = ResolveCurrentUserId(packet);
                if (currentUserId < 0) { SendError("Phiên đăng nhập không hợp lệ."); return; }
                Console.WriteLine($"[Handler] Nhận yêu cầu DOC_LIST từ user ID={currentUserId}...");
                var docs = DocumentRepository.GetByUserId(currentUserId);
                var response = new Payload_DOC_LIST_Response
                {
                    documents = docs.Select(d => new DocInfo
                    {
                        docID = d.Id,
                        title = d.Title,
                        permission = DocumentShareRepository.GetPermission(d.Id, currentUserId) ?? "viewer",
                        updateAt = d.UpdatedAt
                    }).ToList()
                };
                PacketHelper.Send(_stream, Packet.Create(MessageType.DOC_LIST, response));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handler] DOC_LIST error: {ex}");
                SendError("Không thể tải danh sách tài liệu vào lúc này.");
            }
        }

        private void HandleDocCreate(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { SendError("Phiên đăng nhập không hợp lệ."); return; }
            var payload = packet.GetPayload<Payload_DOC_CREATE_Request>();
            string title = (payload?.title ?? string.Empty).Trim();
            string initialContent = payload?.content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title)) title = "Tài liệu không tiêu đề";
            Console.WriteLine($"[Handler] Nhận yêu cầu DOC_CREATE từ user ID={currentUserId}, title='{title}'...");
            string shareCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            var document = new Document { OwnerId = currentUserId, ShareCode = shareCode, Title = title, Content = initialContent, IsPublic = false, PublicPermission = "viewer" };
            string docId = DocumentRepository.Create(document);
            var createdDoc = DocumentRepository.GetById(docId);
            var response = new Payload_DOC_CREATE_Response { docID = docId, title = createdDoc?.Title ?? title, content = createdDoc?.Content ?? string.Empty, revision = DocumentRepository.GetCurrentRevision(docId), shareCode = shareCode };
            PacketHelper.Send(_stream, Packet.Create(MessageType.DOC_CREATE, response));
        }

        private void HandleDocOpen(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { SendError("Phiên đăng nhập không hợp lệ."); return; }
            var payload = packet.GetPayload<Payload_DOC_OPEN_Request>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID)) { SendError("docID không hợp lệ."); return; }
            string docId = payload.docID.Trim();
            Console.WriteLine($"[Handler] Nhận yêu cầu DOC_OPEN từ user ID={currentUserId}, docID={docId}...");
            var document = DocumentRepository.GetById(docId);
            if (document == null) { SendError("Không tìm thấy tài liệu."); return; }
            string permission = DocumentShareRepository.GetPermission(docId, currentUserId);
            if (permission == null) { SendError("Bạn không có quyền truy cập tài liệu này."); return; }
            _currentDocId = docId;
            var response = new Payload_DOC_OPEN_Response { docID = document.Id, title = document.Title, content = document.Content ?? string.Empty, revision = DocumentRepository.GetCurrentRevision(docId), permission = permission };
            PacketHelper.Send(_stream, Packet.Create(MessageType.DOC_OPEN, response));
        }

        private void HandleDocSave(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0) { SendError("Phiên đăng nhập không hợp lệ."); return; }
            var payload = packet.GetPayload<Payload_DOC_SAVE_Request>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID)) { SendError("docID không hợp lệ."); return; }
            string docId = payload.docID.Trim();
            var document = DocumentRepository.GetById(docId);
            if (document == null) { SendError("Không tìm thấy tài liệu."); return; }
            string permission = DocumentShareRepository.GetPermission(docId, currentUserId);
            if (permission != "owner" && permission != "editor") { SendError("Bạn không có quyền lưu tài liệu này."); return; }
            bool updated = DocumentRepository.UpdateContent(docId, payload.content ?? string.Empty);
            if (!updated) { SendError("Lưu tài liệu thất bại."); return; }
            PacketHelper.Send(_stream, Packet.Create(MessageType.OK, new Payload_OK { Message = "Lưu tài liệu thành công" }));
        }

        private void HandleDocLeave(Packet packet)
        {
            var payload = packet.GetPayload<Payload_DOC_LEAVE_Request>();
            string docId = payload?.docID ?? _currentDocId;
            Console.WriteLine($"[Handler] User {_username} (ID={_userId}) left document {docId}.");
            _currentDocId = null;
            if (!string.IsNullOrEmpty(docId))
            {
                bool isStillInUse = SocketServer.GetActiveHandlers().Any(h => h.CurrentDocId == docId);
                if (!isStillInUse) { DocumentStateManager.Remove(docId); Console.WriteLine($"[Handler] Cleaned up state for document {docId} (no active editors)."); }
            }
            PacketHelper.Send(_stream, Packet.Create(MessageType.OK, new Payload_OK { Message = "Left document" }));
        }

        private void HandleDocShare(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            var payload = packet.GetPayload<Payload_DOC_SHARE_Request>();
            if (payload == null || string.IsNullOrEmpty(payload.docID) || string.IsNullOrEmpty(payload.targetUsername)) { SendError("Dữ liệu chia sẻ không hợp lệ."); return; }
            var doc = DocumentRepository.GetById(payload.docID);
            if (doc == null) { SendError("Tài liệu không tồn tại."); return; }
            if (doc.OwnerId != currentUserId) { SendError("Chỉ chủ sở hữu mới có quyền chia sẻ."); return; }
            if (string.IsNullOrEmpty(doc.ShareCode)) { doc.ShareCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(); DocumentRepository.UpdateShareCode(doc.Id, doc.ShareCode); }
            var targetUser = UserRepository.GetByUsername(payload.targetUsername);
            if (targetUser == null) { SendError("Người dùng không tồn tại."); return; }
            string sharePermission = (payload.Permission ?? "editor").ToLower();
            DocumentShareRepository.ShareDocument(payload.docID, targetUser.Id, sharePermission);
            var response = new Payload_DOC_SHARE_Response { success = true, message = $"Đã chia sẻ cho {payload.targetUsername} với quyền {sharePermission}", ShareCode = doc.ShareCode };
            PacketHelper.Send(_stream, Packet.Create(MessageType.DOC_SHARE, response));
        }

        private void HandleDocJoinCode(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            var payload = packet.GetPayload<Payload_DOC_JOIN_CODE_Request>();
            if (payload == null || string.IsNullOrEmpty(payload.shareCode)) { SendError("Mã chia sẻ không hợp lệ."); return; }
            var doc = DocumentRepository.GetByShareCode(payload.shareCode);
            if (doc == null) { SendError("Mã chia sẻ không hợp lệ."); return; }
            if (!doc.IsPublic) { SendError("Tài liệu này không cho phép tham gia bằng mã."); return; }
            string permission = DocumentShareRepository.GetPermission(doc.Id, currentUserId);
            if (permission == null) { permission = doc.PublicPermission ?? "viewer"; DocumentShareRepository.ShareDocument(doc.Id, currentUserId, permission); }
            
            // [FIX] Enable real-time broadcast tracking for public-join users
            _currentDocId = doc.Id;

            var response = new Payload_DOC_JOIN_CODE_Response { docID = doc.Id, title = doc.Title, content = doc.Content ?? string.Empty, revision = DocumentRepository.GetCurrentRevision(doc.Id), permission = permission };
            PacketHelper.Send(_stream, Packet.Create(MessageType.DOC_JOIN_CODE, response));
        }

        private void HandleDocGetShares(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            var payload = packet.GetPayload<Payload_DOC_GET_SHARES_Request>();
            var doc = DocumentRepository.GetById(payload.docID);
            if (doc == null) { SendError("Tài liệu không tồn tại."); return; }
            if (doc.OwnerId != currentUserId) { SendError("Chỉ chủ sở hữu mới có quyền xem danh sách chia sẻ."); return; }
            var collabs = DocumentShareRepository.GetCollaborators(doc.Id);
            var response = new Payload_DOC_GET_SHARES_Response
            {
                docID = doc.Id, shareCode = doc.ShareCode, isPublic = doc.IsPublic, publicPermission = doc.PublicPermission,
                collaborators = collabs.Select(c => new CollaboratorInfo { userId = (int)c.id, username = (string)c.username, email = (string)c.email, permission = (string)c.permission, invitedAt = (DateTime)c.invited_at }).ToList()
            };
            PacketHelper.Send(_stream, Packet.Create(MessageType.DOC_GET_SHARES, response));
        }

        private void HandleDocRevoke(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            var payload = packet.GetPayload<Payload_DOC_REVOKE_Request>();
            var doc = DocumentRepository.GetById(payload.docID);
            if (doc == null) { SendError("Tài liệu không tồn tại."); return; }
            if (doc.OwnerId != currentUserId) { SendError("Chỉ chủ sở hữu mới có quyền thu hồi quyền truy cập."); return; }
            DocumentShareRepository.RevokeAccess(doc.Id, payload.targetUserId);
            PacketHelper.Send(_stream, Packet.Create(MessageType.OK, new Payload_OK { Message = "Đã thu hồi quyền truy cập." }));
        }

        private void HandleDocUpdatePerm(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            var payload = packet.GetPayload<Payload_DOC_UPDATE_PERM_Request>();
            var doc = DocumentRepository.GetById(payload.docID);
            if (doc == null) { SendError("Tài liệu không tồn tại."); return; }
            if (doc.OwnerId != currentUserId) { SendError("Chỉ chủ sở hữu mới có quyền thay đổi quyền truy cập."); return; }
            DocumentShareRepository.UpdatePermission(doc.Id, payload.targetUserId, payload.permission);
            PacketHelper.Send(_stream, Packet.Create(MessageType.OK, new Payload_OK { Message = "Đã cập nhật quyền truy cập." }));
        }

        private void HandleDocSetPublic(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            var payload = packet.GetPayload<Payload_DOC_SET_PUBLIC_Request>();
            var doc = DocumentRepository.GetById(payload.docID);
            if (doc == null) { SendError("Tài liệu không tồn tại."); return; }
            if (doc.OwnerId != currentUserId) { SendError("Chỉ chủ sở hữu mới có quyền thay đổi cài đặt công khai."); return; }
            DocumentRepository.UpdatePublicSettings(doc.Id, payload.isPublic, payload.publicPermission);
            PacketHelper.Send(_stream, Packet.Create(MessageType.OK, new Payload_OK { Message = "Đã cập nhật cài đặt công khai." }));
        }

        private int ResolveCurrentUserId(Packet packet)
        {
            if (!string.IsNullOrWhiteSpace(packet?.Token)) { int id = SessionManager.GetUserId(packet.Token); if (id > 0) return id; }
            return _userId;
        }

        // [OT] Modified to use Operational Transformation
        private void HandleOpInsert(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0)
            {
                Console.WriteLine("[OT] OP_INSERT rejected: not authenticated");
                return;
            }
            var payload = packet.GetPayload<Payload_OP_INSERT>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID))
            {
                Console.WriteLine("[OT] OP_INSERT rejected: invalid docID");
                return;
            }

            var state = DocumentStateManager.GetOrCreate(payload.docID);
            var transformedOps = new List<EditOpItem>();
            foreach (var op in payload.ops ?? new List<EditOpItem>())
            {
                // [ADDED] Detailed OT logging
                Console.WriteLine($"[OT] INSERT user={currentUserId} " +
                    $"clientRev={payload.clientResivion} " +
                    $"serverRev={state.ServerRevision} " +
                    $"pos={op.pos} text='{op.text?.Replace("\n", "\\n").Replace("\r", "\\r")}'");

                var transformedOp = state.TransformAndApply(op, "insert", payload.clientResivion, currentUserId);
                transformedOps.Add(transformedOp);

                // [ADDED] Post-transform logging
                Console.WriteLine($"[OT] INSERT transformed pos={transformedOp.pos} " +
                    $"newServerRev={state.ServerRevision}");
            }

            // [FIX] Send authoritative revision ACK back to client
            PacketHelper.Send(_stream, Packet.Create(MessageType.OK, new Payload_OK { Message = state.ServerRevision.ToString() }));

            // [OT] Broadcast transformed ops to others
            foreach (var tOp in transformedOps)
            {
                var broadcastPayload = new Payload_OP_BROADCAST
                {
                    docID = payload.docID,
                    clientResivion = state.ServerRevision,
                    userID = currentUserId,
                    username = _username,
                    opType = "insert",
                    ops = new List<EditOpItem> { tOp }
                };
                SocketServer.BroadcastToOthers(payload.docID, currentUserId, Packet.Create(MessageType.OP_BROADCAST, broadcastPayload));
            }
        }

        // [OT] Modified to use Operational Transformation
        private void HandleOpDelete(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0)
            {
                Console.WriteLine("[OT] OP_DELETE rejected: not authenticated");
                return;
            }
            var payload = packet.GetPayload<Payload_OP_DELETE>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID))
            {
                Console.WriteLine("[OT] OP_DELETE rejected: invalid docID");
                return;
            }

            var state = DocumentStateManager.GetOrCreate(payload.docID);
            var transformedOps = new List<EditOpItem>();
            foreach (var op in payload.ops ?? new List<EditOpItem>())
            {
                // [ADDED] Detailed OT logging
                Console.WriteLine($"[OT] DELETE user={currentUserId} " +
                    $"clientRev={payload.clientResivion} " +
                    $"serverRev={state.ServerRevision} " +
                    $"pos={op.pos} len={op.text?.Length ?? 0}");

                var transformedOp = state.TransformAndApply(op, "delete", payload.clientResivion, currentUserId);
                transformedOps.Add(transformedOp);

                // [ADDED] Post-transform logging
                Console.WriteLine($"[OT] DELETE transformed pos={transformedOp.pos} " +
                    $"newServerRev={state.ServerRevision}");
            }

            // [FIX] Send authoritative revision ACK back to client
            PacketHelper.Send(_stream, Packet.Create(MessageType.OK, new Payload_OK { Message = state.ServerRevision.ToString() }));

            // [OT] Broadcast transformed ops to others
            foreach (var tOp in transformedOps)
            {
                var broadcastPayload = new Payload_OP_BROADCAST
                {
                    docID = payload.docID,
                    clientResivion = state.ServerRevision,
                    userID = currentUserId,
                    username = _username,
                    opType = "delete",
                    ops = new List<EditOpItem> { tOp }
                };
                SocketServer.BroadcastToOthers(payload.docID, currentUserId, Packet.Create(MessageType.OP_BROADCAST, broadcastPayload));
            }
        }

        private void SendError(string message) { PacketHelper.Send(_stream, Packet.Create(MessageType.ERROR, new Payload_ERROR { Message = message })); }
    }
}
