using System;
using System.Threading.Tasks;
using MarkTogether.Server.Network;

namespace MarkTogether.Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 1. Bật Dapper mapping snake_case (PostgreSQL) ↔ PascalCase (C#)
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║     MarkTogether Server v1.0          ║");
            Console.WriteLine("╠════════════════════════════════════════╣");
            Console.WriteLine("║  PostgreSQL: localhost:5432/myapp_db   ║");
            Console.WriteLine("║  TCP Socket: port 5000                ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();

            // 2. Khởi động TCP Socket Server
            var server = new SocketServer(5000);
            Task.Run(() => server.StartAsync());

            Console.WriteLine("Nhấn Enter để dừng server...");
            Console.ReadLine();
            server.Stop();
        }
    }
}
