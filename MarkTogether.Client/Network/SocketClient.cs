using System;
using System.Collections.Generic;
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

        public Payload_DOC_LIST_Response GetDocuments()
        {
            EnsureAuthenticated();

            var packet = Packet.Create(MessageType.DOC_LIST, new Payload_DOC_LIST_Request());
            packet.Token = Token;
            Send(packet);

            Packet response = Receive();
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
            Send(packet);

            Packet response = Receive();
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
            Send(packet);

            Packet response = Receive();
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
            Send(packet);

            Packet response = Receive();
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

        private void EnsureAuthenticated()
        {
            if (!_connected || string.IsNullOrEmpty(Token))
            {
                throw new InvalidOperationException("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
            }
        }
    }
}
