using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MarkTogether.Shared;

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
        private readonly X509Certificate2 _serverCert;
        private bool _running;

        // [ADDED] Track active client handlers
        private static readonly List<ClientHandler> _activeHandlers = new List<ClientHandler>();
        private static readonly object _handlersLock = new object();

        public SocketServer(int port, X509Certificate2 cert)
        {
            _port = port;
            _serverCert = cert;
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

                    // TLS handshake runs inside the client task so one slow/non-TLS
                    // connection cannot block the accept loop for every other client.
                    _ = Task.Run(() => {
                        ClientHandler handler = null;
                        try
                        {
                            handler = new ClientHandler(tcpClient, _serverCert, clientIp);

                            // [ADDED] Register handler
                            lock (_handlersLock)
                            {
                                _activeHandlers.Add(handler);
                            }

                            handler.ProcessAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Server] Loi xu ly client {clientIp}: {ex.Message}");
                            try { tcpClient.Close(); } catch { }
                        }
                        finally
                        {
                            // [ADDED] Unregister handler when done
                            if (handler != null)
                            {
                                lock (_handlersLock)
                                {
                                    _activeHandlers.Remove(handler);
                                }
                            }
                        }
                    });
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

        // [ADDED] Get copy of active handlers
        public static List<ClientHandler> GetActiveHandlers()
        {
            lock (_handlersLock)
            {
                return _activeHandlers.ToList();
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
