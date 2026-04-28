using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using MarkTogether.Server.Database.Models;
using MarkTogether.Server.Database.Repositories;
using MarkTogether.Server.Services;
using MarkTogether.Shared;

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

        public int UserId => _userId;
        public string Username => _username;

        public ClientHandler(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
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

                    try
                    {
                        // 2. Dispatch theo MessageType
                        switch (packet.Type)
                        {
                            case MessageType.AUTH_REGISTER:
                                HandleRegister(packet);
                                break;

                            case MessageType.AUTH_LOGIN:
                                HandleLogin(packet);
                                break;

                            case MessageType.DOC_LIST:
                                HandleDocList(packet);
                                break;

                            case MessageType.DOC_CREATE:
                                HandleDocCreate(packet);
                                break;

                            case MessageType.DOC_OPEN:
                                HandleDocOpen(packet);
                                break;

                            case MessageType.DOC_SAVE:
                                HandleDocSave(packet);
                                break;

                            case MessageType.OP_INSERT:
                                HandleOpInsert(packet);
                                break;

                            case MessageType.OP_DELETE:
                                HandleOpDelete(packet);
                                break;

                            // ═══════════════════════════════════════
                            // TODO: Thêm các case khác ở đây sau này:
                            // case MessageType.DOC_CREATE:
                            // case MessageType.DOC_JOIN:
                            // case MessageType.OP_INSERT:
                            // case MessageType.OP_DELETE:
                            // ═══════════════════════════════════════

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

            // 1. Parse payload từ client
            var payload = packet.GetPayload<Payload_AUTH_REGISTER>();

            // 2. Gọi AuthService xử lý logic
            Payload_AUTH_RESPONSE response = AuthService.Register(
                payload.Username, payload.Email, payload.Password
            );

            // 3. Nếu thành công → lưu session
            if (response.Success)
            {
                _token = response.Token;
                _userId = response.UserId;
                _username = response.Username;
                SessionManager.AddSession(_token, _userId);
            }

            // 4. Gửi kết quả về cho client
            var responsePacket = Packet.Create(MessageType.AUTH_RESPONSE, response);
            PacketHelper.Send(_stream, responsePacket);
        }

        // ═══════════════════════════════════════════
        //  XỬ LÝ ĐĂNG NHẬP
        // ═══════════════════════════════════════════
        private void HandleLogin(Packet packet)
        {
            Console.WriteLine("[Handler] Nhận yêu cầu ĐĂNG NHẬP...");

            // 1. Parse payload
            var payload = packet.GetPayload<Payload_AUTH_LOGIN>();

            // 2. Gọi AuthService
            Payload_AUTH_RESPONSE response = AuthService.Login(
                payload.Username, payload.Password
            );

            // 3. Nếu thành công → lưu session
            if (response.Success)
            {
                _token = response.Token;
                _userId = response.UserId;
                _username = response.Username;
                SessionManager.AddSession(_token, _userId);
            }

            // 4. Trả kết quả
            var responsePacket = Packet.Create(MessageType.AUTH_RESPONSE, response);
            PacketHelper.Send(_stream, responsePacket);
        }

        private void HandleDocList(Packet packet)
        {
            try
            {
                int currentUserId = ResolveCurrentUserId(packet);
                if (currentUserId < 0)
                {
                    SendError("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
                    return;
                }

                Console.WriteLine($"[Handler] Nhận yêu cầu DOC_LIST từ user ID={currentUserId}...");

                var docs = DocumentRepository.GetByUserId(currentUserId);
                var response = new Payload_DOC_LIST_Response
                {
                    documents = docs.Select(d => new DocInfo
                    {
                        docID = d.Id,
                        title = d.Title,
                        permission = d.OwnerId == currentUserId ? "owner" : "viewer",
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
            if (currentUserId < 0)
            {
                SendError("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
                return;
            }

            var payload = packet.GetPayload<Payload_DOC_CREATE_Request>();
            string title = (payload?.title ?? string.Empty).Trim();
            string initialContent = payload?.content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "Tài liệu không tiêu đề";
            }

            Console.WriteLine($"[Handler] Nhận yêu cầu DOC_CREATE từ user ID={currentUserId}, title='{title}'...");

            var document = new Document
            {
                OwnerId = currentUserId,
                Title = title,
                Content = initialContent
            };

            string docId = DocumentRepository.Create(document);
            var createdDoc = DocumentRepository.GetById(docId);

            var response = new Payload_DOC_CREATE_Response
            {
                docID = docId,
                title = createdDoc?.Title ?? title,
                content = createdDoc?.Content ?? string.Empty,
                revision = DocumentRepository.GetCurrentRevision(docId),
                shareCode = null
            };

            PacketHelper.Send(_stream, Packet.Create(MessageType.DOC_CREATE, response));
        }

        private void HandleDocOpen(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0)
            {
                SendError("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
                return;
            }

            var payload = packet.GetPayload<Payload_DOC_OPEN_Request>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID))
            {
                SendError("docID không hợp lệ.");
                return;
            }

            string docId = payload.docID.Trim();
            Console.WriteLine($"[Handler] Nhận yêu cầu DOC_OPEN từ user ID={currentUserId}, docID={docId}...");

            var document = DocumentRepository.GetById(docId);
            if (document == null)
            {
                SendError("Không tìm thấy tài liệu.");
                return;
            }

            if (document.OwnerId != currentUserId)
            {
                SendError("Bạn không có quyền truy cập tài liệu này.");
                return;
            }

            var response = new Payload_DOC_OPEN_Response
            {
                docID = document.Id,
                title = document.Title,
                content = document.Content ?? string.Empty,
                revision = DocumentRepository.GetCurrentRevision(docId),
                permission = "owner"
            };

            PacketHelper.Send(_stream, Packet.Create(MessageType.DOC_OPEN, response));
        }

        private void HandleDocSave(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0)
            {
                SendError("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
                return;
            }

            var payload = packet.GetPayload<Payload_DOC_SAVE_Request>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID))
            {
                SendError("docID không hợp lệ.");
                return;
            }

            string docId = payload.docID.Trim();
            var document = DocumentRepository.GetById(docId);
            if (document == null)
            {
                SendError("Không tìm thấy tài liệu.");
                return;
            }

            if (document.OwnerId != currentUserId)
            {
                SendError("Bạn không có quyền lưu tài liệu này.");
                return;
            }

            bool updated = DocumentRepository.UpdateContent(docId, payload.content ?? string.Empty);
            if (!updated)
            {
                SendError("Lưu tài liệu thất bại.");
                return;
            }

            PacketHelper.Send(_stream, Packet.Create(MessageType.OK, new Payload_OK
            {
                Message = "Lưu tài liệu thành công"
            }));
        }

        private int ResolveCurrentUserId(Packet packet)
        {
            if (!string.IsNullOrWhiteSpace(packet?.Token))
            {
                int tokenUserId = SessionManager.GetUserId(packet.Token);
                if (tokenUserId > 0)
                {
                    return tokenUserId;
                }
            }

            return _userId;
        }

        private void HandleOpInsert(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0)
            {
                SendError("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
                return;
            }

            var payload = packet.GetPayload<Payload_OP_INSERT>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID))
            {
                SendError("docID không hợp lệ.");
                return;
            }

            int charCount = (payload.ops ?? new List<EditOpItem>())
                .Sum(op => op?.text?.Length ?? 0);

            if (charCount > 5)
            {
                SendError("OP_INSERT chỉ được chứa tối đa 5 ký tự trong một gói.");
                return;
            }

            Console.WriteLine(
                "[Handler] OP_INSERT received\n" +
                $"  user={currentUserId}\n" +
                $"  docID={payload.docID}\n" +
                $"  clientRevision={payload.clientResivion}\n" +
                $"  totalChars={charCount}\n" +
                $"  opsCount={payload.ops?.Count ?? 0}\n" +
                $"{BuildOpsDebug(payload.ops)}");

            PacketHelper.Send(_stream, Packet.Create(MessageType.OK, new Payload_OK
            {
                Message = "OP_INSERT received"
            }));
        }

        private void HandleOpDelete(Packet packet)
        {
            int currentUserId = ResolveCurrentUserId(packet);
            if (currentUserId < 0)
            {
                SendError("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
                return;
            }

            var payload = packet.GetPayload<Payload_OP_DELETE>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.docID))
            {
                SendError("docID không hợp lệ.");
                return;
            }

            int charCount = (payload.ops ?? new List<EditOpItem>())
                .Sum(op => op?.text?.Length ?? 0);

            if (charCount > 5)
            {
                SendError("OP_DELETE chỉ được chứa tối đa 5 ký tự trong một gói.");
                return;
            }

            Console.WriteLine(
                "[Handler] OP_DELETE received\n" +
                $"  user={currentUserId}\n" +
                $"  docID={payload.docID}\n" +
                $"  clientRevision={payload.clientResivion}\n" +
                $"  totalChars={charCount}\n" +
                $"  opsCount={payload.ops?.Count ?? 0}\n" +
                $"{BuildOpsDebug(payload.ops)}");

            PacketHelper.Send(_stream, Packet.Create(MessageType.OK, new Payload_OK
            {
                Message = "OP_DELETE received"
            }));
        }

        private string BuildOpsDebug(List<EditOpItem> ops)
        {
            if (ops == null || ops.Count == 0)
            {
                return "  ops=[]";
            }

            var lines = ops.Select((op, i) =>
            {
                if (op == null)
                {
                    return $"  op[{i}] = <null>";
                }

                string safeText = (op.text ?? string.Empty)
                    .Replace("\\", "\\\\")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");

                return $"  op[{i}] pos={op.pos}, len={safeText.Length}, text=\"{safeText}\", ts={op.timestamp:O}";
            });

            return string.Join(Environment.NewLine, lines);
        }

        // ═══════════════════════════════════════════
        //  GỬI LỖI
        // ═══════════════════════════════════════════
        private void SendError(string message)
        {
            var errorPacket = Packet.Create(MessageType.ERROR, new Payload_ERROR { Message = message });
            PacketHelper.Send(_stream, errorPacket);
        }
    }
}
