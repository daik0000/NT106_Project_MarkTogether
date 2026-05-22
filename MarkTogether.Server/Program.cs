using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MarkTogether.Server.Network;

namespace MarkTogether.Server
{
    internal class Program
    {
        private static readonly Dictionary<string, string> FallbackAssemblyPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "System.Text.Json", @"..\..\..\packages\System.Text.Json.4.7.2\lib\net461\System.Text.Json.dll" },
            { "Microsoft.Bcl.AsyncInterfaces", @"..\..\..\packages\Microsoft.Bcl.AsyncInterfaces.9.0.1\lib\net462\Microsoft.Bcl.AsyncInterfaces.dll" },
            { "System.Memory", @"..\..\..\packages\System.Memory.4.6.3\lib\net462\System.Memory.dll" },
            { "System.Buffers", @"..\..\..\packages\System.Buffers.4.6.1\lib\net462\System.Buffers.dll" },
            { "System.Runtime.CompilerServices.Unsafe", @"..\..\..\packages\System.Runtime.CompilerServices.Unsafe.6.1.2\lib\net462\System.Runtime.CompilerServices.Unsafe.dll" },
            { "System.Threading.Tasks.Extensions", @"..\..\..\packages\System.Threading.Tasks.Extensions.4.5.4\lib\net461\System.Threading.Tasks.Extensions.dll" },
            { "System.Numerics.Vectors", @"..\..\..\packages\System.Numerics.Vectors.4.6.1\lib\net462\System.Numerics.Vectors.dll" }
        };

        private static readonly System.Threading.ManualResetEvent _quitEvent = new System.Threading.ManualResetEvent(false);

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveMissingAssembly;

            bool isService = false;
            foreach (var arg in args)
            {
                if (arg.Equals("--service", StringComparison.OrdinalIgnoreCase))
                {
                    isService = true;
                }
            }

            // 1. Bật Dapper mapping snake_case (PostgreSQL) ↔ PascalCase (C#)
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            int port = ResolvePort();

            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║     MarkTogether Server v1.0          ║");
            Console.WriteLine("╠════════════════════════════════════════╣");
            Console.WriteLine($"║  TCP Socket: port {port,-5}             ║");
            Console.WriteLine($"║  Mode: {(isService ? "Service" : "Interactive"),-20}║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();

            // 2. Khởi động TCP Socket Server
            var server = new SocketServer(port);
            Task.Run(() => server.StartAsync());

            if (isService)
            {
                Console.WriteLine("[Server] Chạy dưới dạng service. Nhấn Ctrl+C để dừng.");
                
                // Xử lý Graceful Shutdown trên Linux (SIGTERM/SIGINT) và Windows
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    _quitEvent.Set();
                };

                _quitEvent.WaitOne();
            }
            else
            {
                Console.WriteLine("Nhấn Enter để dừng server...");
                Console.ReadLine();
            }

            Console.WriteLine("[Server] Đang tắt...");
            server.Stop();
        }

        private static int ResolvePort()
        {
            const int defaultPort = 5000;
            string rawPort = Environment.GetEnvironmentVariable("MARKTOGETHER_PORT");

            if (string.IsNullOrWhiteSpace(rawPort))
                return defaultPort;

            int parsedPort;
            if (!int.TryParse(rawPort, out parsedPort) || parsedPort < 1 || parsedPort > 65535)
            {
                Console.WriteLine($"[Config] MARKTOGETHER_PORT không hợp lệ ('{rawPort}'), fallback về {defaultPort}.");
                return defaultPort;
            }

            return parsedPort;
        }

        private static Assembly ResolveMissingAssembly(object sender, ResolveEventArgs args)
        {
            var missingAssembly = new AssemblyName(args.Name).Name;
            if (!FallbackAssemblyPaths.TryGetValue(missingAssembly, out var relativePath))
                return null;

            string dllPath = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                relativePath));

            return File.Exists(dllPath) ? Assembly.LoadFrom(dllPath) : null;
        }
    }
}
