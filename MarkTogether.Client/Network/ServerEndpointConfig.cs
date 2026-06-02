using System;
using System.IO;

namespace MarkTogether.Client.Network
{
    public sealed class ServerEndpointConfig
    {
        public string Host { get; private set; } = "localhost";
        public int Port { get; private set; } = 5000;
        public string CertThumb { get; private set; } = string.Empty;

        public static ServerEndpointConfig Load()
        {
            var cfg = new ServerEndpointConfig();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.config");

            if (!File.Exists(path))
            {
                ApplyEnvOverrides(cfg);
                return cfg;
            }

            foreach (var rawLine in File.ReadAllLines(path))
            {
                string line = (rawLine ?? string.Empty).Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                if (line.StartsWith("HOST=", StringComparison.OrdinalIgnoreCase))
                {
                    string host = line.Substring("HOST=".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(host))
                    {
                        cfg.Host = host;
                    }
                    continue;
                }

                if (line.StartsWith("PORT=", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(line.Substring("PORT=".Length).Trim(), out int port) &&
                        port > 0 && port <= 65535)
                    {
                        cfg.Port = port;
                    }
                    continue;
                }

                if (line.StartsWith("CERT_THUMB=", StringComparison.OrdinalIgnoreCase))
                {
                    cfg.CertThumb = line.Substring("CERT_THUMB=".Length).Trim();
                }
            }

            ApplyEnvOverrides(cfg);
            return cfg;
        }

        private static void ApplyEnvOverrides(ServerEndpointConfig cfg)
        {
            string host = Environment.GetEnvironmentVariable("MARKTOGETHER_CLIENT_HOST");
            if (!string.IsNullOrWhiteSpace(host))
            {
                cfg.Host = host.Trim();
            }

            string portText = Environment.GetEnvironmentVariable("MARKTOGETHER_CLIENT_PORT");
            if (int.TryParse(portText, out int port) && port > 0 && port <= 65535)
            {
                cfg.Port = port;
            }

            string thumb = Environment.GetEnvironmentVariable("MARKTOGETHER_CLIENT_CERT_THUMB");
            if (thumb != null)
            {
                cfg.CertThumb = thumb.Trim();
            }
        }
    }
}
