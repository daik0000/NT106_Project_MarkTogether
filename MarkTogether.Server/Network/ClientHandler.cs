using System;
using System.IO;
using System.Net.Sockets;
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

                    // 2. Dispatch theo MessageType
                    switch (packet.Type)
                    {
                        case MessageType.AUTH_REGISTER:
                            HandleRegister(packet);
                            break;

                        case MessageType.AUTH_LOGIN:
                            HandleLogin(packet);
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
            }
            catch (IOException)
            {
                // Client ngắt kết nối bình thường
                Console.WriteLine($"[Handler] Client {_username ?? "unknown"} (ID={_userId}) đã ngắt kết nối.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handler] Lỗi: {ex.Message}");
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
            var payload = packet.GetPayload<RegisterPayload>();

            // 2. Gọi AuthService xử lý logic
            AuthResponse response = AuthService.Register(
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
            var payload = packet.GetPayload<AuthPayload>();

            // 2. Gọi AuthService
            AuthResponse response = AuthService.Login(
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

        // ═══════════════════════════════════════════
        //  GỬI LỖI
        // ═══════════════════════════════════════════
        private void SendError(string message)
        {
            var errorPacket = Packet.Create(MessageType.ERROR, new ErrorPayload { Message = message });
            PacketHelper.Send(_stream, errorPacket);
        }
    }
}
