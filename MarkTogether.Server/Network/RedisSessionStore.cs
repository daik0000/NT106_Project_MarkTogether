using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace MarkTogether.Server.Network
{
    public sealed class RedisSessionStore : ISessionStore
    {
        public bool RemoveOnDisconnect => false;

        private readonly string _host;
        private readonly int _port;
        private readonly string _password;
        private readonly int _sessionTtlSeconds;
        private readonly object _sync = new object();
        private TcpClient _client;
        private NetworkStream _stream;

        public RedisSessionStore(string connectionString, int sessionTtlSeconds)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Redis connection string is empty.", nameof(connectionString));
            }

            var parsed = ParseConnectionString(connectionString);
            _host = parsed.Host;
            _port = parsed.Port;
            _password = parsed.Password;
            _sessionTtlSeconds = sessionTtlSeconds > 0 ? sessionTtlSeconds : 86400;
        }

        public void AddOrUpdate(string token, int userId)
        {
            if (string.IsNullOrEmpty(token)) return;

            lock (_sync)
            {
                Execute("SET", BuildKey(token), userId.ToString(CultureInfo.InvariantCulture), "EX",
                    _sessionTtlSeconds.ToString(CultureInfo.InvariantCulture));
            }
        }

        public int GetUserId(string token)
        {
            if (string.IsNullOrEmpty(token)) return -1;

            lock (_sync)
            {
                object result = Execute("GET", BuildKey(token));
                if (!(result is byte[] bytes) || bytes.Length == 0)
                {
                    return -1;
                }

                string raw = Encoding.UTF8.GetString(bytes);
                return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int userId)
                    ? userId
                    : -1;
            }
        }

        public void Remove(string token)
        {
            if (string.IsNullOrEmpty(token)) return;

            lock (_sync)
            {
                Execute("DEL", BuildKey(token));
            }
        }

        public bool Exists(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;

            lock (_sync)
            {
                object result = Execute("EXISTS", BuildKey(token));
                if (result is long n) return n > 0;
                return false;
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                CloseConnection();
            }
        }

        private object Execute(params string[] args)
        {
            EnsureConnected();

            try
            {
                WriteCommand(args);
                return ReadResp();
            }
            catch
            {
                CloseConnection();
                throw;
            }
        }

        private void EnsureConnected()
        {
            if (_client != null && _client.Connected && _stream != null)
            {
                return;
            }

            CloseConnection();

            _client = new TcpClient();
            _client.Connect(_host, _port);
            _stream = _client.GetStream();
            _stream.ReadTimeout = 5000;
            _stream.WriteTimeout = 5000;

            if (!string.IsNullOrWhiteSpace(_password))
            {
                WriteCommand(new[] { "AUTH", _password });
                ReadResp();
            }
        }

        private void WriteCommand(IReadOnlyList<string> args)
        {
            var sb = new StringBuilder();
            sb.Append('*').Append(args.Count).Append("\r\n");
            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i] ?? string.Empty;
                byte[] data = Encoding.UTF8.GetBytes(arg);
                sb.Append('$').Append(data.Length).Append("\r\n");
                sb.Append(arg).Append("\r\n");
            }

            byte[] payload = Encoding.UTF8.GetBytes(sb.ToString());
            _stream.Write(payload, 0, payload.Length);
            _stream.Flush();
        }

        private object ReadResp()
        {
            int marker = _stream.ReadByte();
            if (marker < 0)
            {
                throw new IOException("Redis closed connection.");
            }

            switch ((char)marker)
            {
                case '+':
                    return ReadLine();
                case '-':
                    throw new IOException("Redis error: " + ReadLine());
                case ':':
                    {
                        string line = ReadLine();
                        return long.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out long n) ? n : 0L;
                    }
                case '$':
                    {
                        string line = ReadLine();
                        if (!int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out int length))
                        {
                            throw new IOException("Invalid Redis bulk length.");
                        }
                        if (length < 0) return null;
                        byte[] data = ReadExact(length);
                        ReadExact(2); // CRLF
                        return data;
                    }
                case '*':
                    {
                        string line = ReadLine();
                        if (!int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count))
                        {
                            throw new IOException("Invalid Redis array length.");
                        }
                        if (count < 0) return null;
                        var values = new object[count];
                        for (int i = 0; i < count; i++)
                        {
                            values[i] = ReadResp();
                        }
                        return values;
                    }
                default:
                    throw new IOException("Unknown Redis response marker.");
            }
        }

        private string ReadLine()
        {
            var ms = new MemoryStream();
            while (true)
            {
                int b = _stream.ReadByte();
                if (b < 0) throw new IOException("Redis closed connection while reading line.");
                if (b == '\r')
                {
                    int next = _stream.ReadByte();
                    if (next == '\n')
                    {
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                    if (next < 0) throw new IOException("Invalid Redis line ending.");
                    ms.WriteByte((byte)b);
                    ms.WriteByte((byte)next);
                    continue;
                }
                ms.WriteByte((byte)b);
            }
        }

        private byte[] ReadExact(int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = _stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                {
                    throw new IOException("Redis closed connection while reading bulk value.");
                }
                offset += read;
            }
            return buffer;
        }

        private void CloseConnection()
        {
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null;
            _client = null;
        }

        private static string BuildKey(string token)
        {
            return "mt:session:" + token;
        }

        private static RedisConnectionInfo ParseConnectionString(string connectionString)
        {
            string text = connectionString.Trim();
            string[] segments = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                throw new InvalidOperationException("Redis connection string is invalid.");
            }

            string endpoint = segments[0].Trim();
            if (endpoint.StartsWith("redis://", StringComparison.OrdinalIgnoreCase))
            {
                endpoint = endpoint.Substring("redis://".Length);
            }

            int colon = endpoint.LastIndexOf(':');
            if (colon <= 0 || colon >= endpoint.Length - 1)
            {
                throw new InvalidOperationException("Redis endpoint phải có dạng host:port.");
            }

            string host = endpoint.Substring(0, colon).Trim();
            string portText = endpoint.Substring(colon + 1).Trim();
            if (!int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port))
            {
                throw new InvalidOperationException("Redis port không hợp lệ.");
            }

            string password = string.Empty;
            for (int i = 1; i < segments.Length; i++)
            {
                string part = segments[i].Trim();
                int idx = part.IndexOf('=');
                if (idx <= 0) continue;

                string key = part.Substring(0, idx).Trim();
                string value = part.Substring(idx + 1).Trim();
                if (key.Equals("password", StringComparison.OrdinalIgnoreCase))
                {
                    password = value;
                }
            }

            return new RedisConnectionInfo
            {
                Host = host,
                Port = port,
                Password = password
            };
        }

        private sealed class RedisConnectionInfo
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public string Password { get; set; }
        }
    }
}
