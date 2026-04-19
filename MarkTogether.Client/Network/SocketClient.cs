using System;
using System.Net.Sockets;
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

        // Singleton instance
        public static SocketClient Instance { get; } = new SocketClient();

        // Thông tin user sau khi login thành công
        public string Token { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        /// <summary>
        /// Kết nối đến server.
        /// </summary>
        public void Connect(string host = "localhost", int port = 5000)
        {
            if (_connected) return; // Đã kết nối rồi thì bỏ qua

            _tcp = new TcpClient();
            _tcp.Connect(host, port);
            _stream = _tcp.GetStream();
            _connected = true;
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
            return PacketHelper.Receive(_stream);
        }

        /// <summary>
        /// Đăng nhập. Trả Payload_AUTH_RESPONSE.
        /// </summary>
        public Payload_AUTH_RESPONSE Login(string username, string password)
        {
            var packet = Packet.Create(MessageType.AUTH_LOGIN,
                new Payload_AUTH_LOGIN { Username = username, Password = password });
            Send(packet);

            Packet response = Receive();
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
            Send(packet);

            Packet response = Receive();
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
        /// Ngắt kết nối.
        /// </summary>
        public void Disconnect()
        {
            _connected = false;
            Token = null;
            _stream?.Close();
            _tcp?.Close();
        }
    }
}
