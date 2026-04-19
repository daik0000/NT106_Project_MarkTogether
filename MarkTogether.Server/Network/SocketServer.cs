using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MarkTogether.Server.Network
{
    /// <summary>
    /// TCP Socket Server: lắng nghe kết nối client trên port chỉ định,
    /// mỗi client được bàn giao cho một ClientHandler riêng chạy song song.
    /// </summary>
    public class SocketServer
    {
        private TcpListener _listener;
        private readonly int _port;
        private bool _running;

        public SocketServer(int port = 5000)
        {
            _port = port;
        }

        /// <summary>
        /// Khởi động server, vòng lặp chấp nhận client liên tục.
        /// </summary>
        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _running = true;

            Console.WriteLine($"[Server] Đang lắng nghe trên port {_port}...");

            while (_running)
            {
                try
                {
                    // Chờ client kết nối (không block thread nhờ async)
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
                    string clientIp = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                    Console.WriteLine($"[Server] Client mới kết nối: {clientIp}");

                    // Tạo handler riêng cho client này, xử lý song song
                    var handler = new ClientHandler(tcpClient);
                    Task.Run(() => handler.ProcessAsync());
                }
                catch (ObjectDisposedException)
                {
                    // Listener đã bị Stop() → bỏ qua
                }
                catch (Exception ex)
                {
                    if (_running)
                        Console.WriteLine($"[Server] Lỗi accept: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Dừng server.
        /// </summary>
        public void Stop()
        {
            _running = false;
            _listener?.Stop();
            Console.WriteLine("[Server] Đã dừng.");
        }
    }
}
