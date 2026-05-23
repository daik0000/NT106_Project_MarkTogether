using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MarkTogether.Gateway
{
    internal sealed class GatewayConfig
    {
        public string ListenHost { get; private set; } = "0.0.0.0";
        public int ListenPort { get; private set; } = 5000;
        public string TlsCertPath { get; private set; }
        public string TlsCertPassword { get; private set; }
        public bool UpstreamTls { get; private set; } = true;
        public int HealthCheckIntervalSeconds { get; private set; } = 5;
        public int DocRemapTimeoutSeconds { get; private set; } = 120;
        public int UpstreamConnectTimeoutMs { get; private set; } = 5000;
        public List<BackendEndpoint> Backends { get; } = new List<BackendEndpoint>();

        public static GatewayConfig Load(string explicitPath = null)
        {
            var config = new GatewayConfig();
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string path = explicitPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gateway.config");
            }

            if (File.Exists(path))
            {
                foreach (var raw in File.ReadAllLines(path))
                {
                    string line = (raw ?? string.Empty).Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();
                    map[key] = value;
                }
            }

            ApplyEnvOverride(map, "LISTEN_HOST", "MARKTOGETHER_LB_LISTEN_HOST");
            ApplyEnvOverride(map, "LISTEN_PORT", "MARKTOGETHER_LB_LISTEN_PORT");
            ApplyEnvOverride(map, "TLS_CERT_PATH", "MARKTOGETHER_LB_CERT_PATH");
            ApplyEnvOverride(map, "TLS_CERT_PASSWORD", "MARKTOGETHER_LB_CERT_PASSWORD");
            ApplyEnvOverride(map, "BACKENDS", "MARKTOGETHER_LB_BACKENDS");
            ApplyEnvOverride(map, "UPSTREAM_TLS", "MARKTOGETHER_LB_UPSTREAM_TLS");
            ApplyEnvOverride(map, "HEALTHCHECK_INTERVAL_SECONDS", "MARKTOGETHER_LB_HEALTHCHECK_INTERVAL_SECONDS");
            ApplyEnvOverride(map, "DOC_REMAP_TIMEOUT_SECONDS", "MARKTOGETHER_LB_DOC_REMAP_TIMEOUT_SECONDS");
            ApplyEnvOverride(map, "UPSTREAM_CONNECT_TIMEOUT_MS", "MARKTOGETHER_LB_CONNECT_TIMEOUT_MS");

            config.ListenHost = GetString(map, "LISTEN_HOST", config.ListenHost);
            config.ListenPort = GetInt(map, "LISTEN_PORT", config.ListenPort, 1, 65535);
            config.TlsCertPath = GetString(map, "TLS_CERT_PATH", string.Empty);
            config.TlsCertPassword = GetString(map, "TLS_CERT_PASSWORD", string.Empty);
            config.UpstreamTls = GetBool(map, "UPSTREAM_TLS", config.UpstreamTls);
            config.HealthCheckIntervalSeconds = GetInt(map, "HEALTHCHECK_INTERVAL_SECONDS", config.HealthCheckIntervalSeconds, 1, 300);
            config.DocRemapTimeoutSeconds = GetInt(map, "DOC_REMAP_TIMEOUT_SECONDS", config.DocRemapTimeoutSeconds, 1, 86400);
            config.UpstreamConnectTimeoutMs = GetInt(map, "UPSTREAM_CONNECT_TIMEOUT_MS", config.UpstreamConnectTimeoutMs, 500, 30000);

            string backendList = GetString(map, "BACKENDS", string.Empty);
            foreach (var item in backendList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (BackendEndpoint.TryParse(item, out var endpoint))
                {
                    config.Backends.Add(endpoint);
                }
            }

            if (config.Backends.Count == 0)
            {
                throw new InvalidOperationException("Gateway cần ít nhất 1 backend trong BACKENDS.");
            }

            if (string.IsNullOrWhiteSpace(config.TlsCertPath))
            {
                throw new InvalidOperationException("Thiếu TLS_CERT_PATH (hoặc MARKTOGETHER_LB_CERT_PATH).");
            }

            if (!Path.IsPathRooted(config.TlsCertPath))
            {
                config.TlsCertPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.TlsCertPath);
            }

            return config;
        }

        private static void ApplyEnvOverride(Dictionary<string, string> map, string key, string envKey)
        {
            string envValue = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                map[key] = envValue.Trim();
            }
        }

        private static string GetString(Dictionary<string, string> map, string key, string fallback)
        {
            return map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : fallback;
        }

        private static int GetInt(Dictionary<string, string> map, string key, int fallback, int min, int max)
        {
            if (!map.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return fallback;
            }

            if (parsed < min) return min;
            if (parsed > max) return max;
            return parsed;
        }

        private static bool GetBool(Dictionary<string, string> map, string key, bool fallback)
        {
            if (!map.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            string value = raw.Trim();
            if (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return fallback;
        }
    }

    internal sealed class BackendEndpoint
    {
        public string Host { get; set; }
        public int Port { get; set; }

        public override string ToString()
        {
            return Host + ":" + Port;
        }

        public static bool TryParse(string raw, out BackendEndpoint endpoint)
        {
            endpoint = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string text = raw.Trim();
            int idx = text.LastIndexOf(':');
            if (idx <= 0 || idx >= text.Length - 1) return false;

            string host = text.Substring(0, idx).Trim();
            string portText = text.Substring(idx + 1).Trim();
            if (host.Length == 0) return false;
            if (!int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port)) return false;
            if (port <= 0 || port > 65535) return false;

            endpoint = new BackendEndpoint { Host = host, Port = port };
            return true;
        }
    }
}
