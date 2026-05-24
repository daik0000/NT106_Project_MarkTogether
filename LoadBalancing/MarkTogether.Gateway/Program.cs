using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTogether.Gateway
{
    internal static class Program
    {
        private static int _sessionCounter;

        private static int Main(string[] args)
        {
            return MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task<int> MainAsync(string[] args)
        {
            try
            {
                string configPath = null;
                if (args != null)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (string.Equals(args[i], "--config", StringComparison.OrdinalIgnoreCase) &&
                            i + 1 < args.Length)
                        {
                            configPath = args[i + 1];
                        }
                    }
                }

                GatewayConfig config = GatewayConfig.Load(configPath);
                var cert = new X509Certificate2(config.TlsCertPath, config.TlsCertPassword);

                var router = new GatewayRouter(
                    config.Backends,
                    TimeSpan.FromSeconds(config.DocRemapTimeoutSeconds));

                Console.WriteLine($"[{DateTime.UtcNow:O}] [LB] Starting on {config.ListenHost}:{config.ListenPort}");
                Console.WriteLine($"[{DateTime.UtcNow:O}] [LB] Backends: {string.Join(", ", config.Backends)}");
                Console.WriteLine($"[{DateTime.UtcNow:O}] [LB] Upstream TLS: {config.UpstreamTls}");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (s, e) =>
                    {
                        e.Cancel = true;
                        cts.Cancel();
                    };

                    Task healthTask = Task.Run(() => HealthCheckLoopAsync(router, config, cts.Token), cts.Token);

                    var listener = new TcpListener(IPAddress.Parse(config.ListenHost), config.ListenPort);
                    listener.Start();

                    try
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            TcpClient client = await AcceptWithCancelAsync(listener, cts.Token).ConfigureAwait(false);
                            if (client == null) break;

                            int sessionId = Interlocked.Increment(ref _sessionCounter);
                            var session = new ProxySession(sessionId, client, cert, config, router);
                            _ = Task.Run(() => session.RunAsync(), cts.Token);
                        }
                    }
                    finally
                    {
                        try { listener.Stop(); } catch { }
                        cts.Cancel();
                        try { await healthTask.ConfigureAwait(false); } catch { }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[{DateTime.UtcNow:O}] [LB] Fatal: {ex}");
                return 1;
            }
        }

        private static async Task HealthCheckLoopAsync(GatewayRouter router, GatewayConfig config, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                for (int i = 0; i < router.Nodes.Count; i++)
                {
                    bool healthy = await ProbeBackendAsync(router.GetNode(i)?.Endpoint, config, token).ConfigureAwait(false);
                    router.MarkBackendHealth(i, healthy);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(config.HealthCheckIntervalSeconds), token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private static async Task<bool> ProbeBackendAsync(BackendEndpoint endpoint, GatewayConfig config, CancellationToken token)
        {
            if (endpoint == null) return false;

            try
            {
                using (var tcp = new TcpClient())
                {
                    var connectTask = tcp.ConnectAsync(endpoint.Host, endpoint.Port);
                    var timeoutTask = Task.Delay(config.UpstreamConnectTimeoutMs, token);
                    Task winner = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                    if (winner != connectTask || !tcp.Connected)
                    {
                        return false;
                    }

                    if (config.UpstreamTls)
                    {
                        var ssl = new SslStream(tcp.GetStream(), false, (sender, cert, chain, errors) => true);
                        ssl.AuthenticateAsClient(endpoint.Host, null, SslProtocols.Tls12, false);
                        ssl.Close();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<TcpClient> AcceptWithCancelAsync(TcpListener listener, CancellationToken token)
        {
            try
            {
                var acceptTask = listener.AcceptTcpClientAsync();
                var cancelTask = Task.Delay(Timeout.Infinite, token);
                Task winner = await Task.WhenAny(acceptTask, cancelTask).ConfigureAwait(false);
                if (winner == acceptTask)
                {
                    return acceptTask.Result;
                }
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException)
            {
                return null;
            }

            return null;
        }
    }
}
