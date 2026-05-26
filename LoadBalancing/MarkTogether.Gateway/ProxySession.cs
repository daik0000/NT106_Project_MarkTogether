using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MarkTogether.Shared;
using Newtonsoft.Json.Linq;

namespace MarkTogether.Gateway
{
    internal sealed class ProxySession
    {
        private readonly int _sessionId;
        private readonly TcpClient _clientTcp;
        private readonly X509Certificate2 _lbCert;
        private readonly GatewayConfig _config;
        private readonly GatewayRouter _router;

        private readonly object _clientSendLock = new object();
        private readonly object _upstreamSwitchLock = new object();
        private readonly object _upstreamSendLock = new object();

        private SslStream _clientStream;
        private TcpClient _upstreamTcp;
        private Stream _upstreamStream;
        private CancellationTokenSource _upstreamCts;
        private Task _upstreamPumpTask;
        private int _currentBackendIndex = -1;
        private bool _closed;

        private string _activeDocId;
        private int _activeDocBackendIndex = -1;

        public ProxySession(
            int sessionId,
            TcpClient clientTcp,
            X509Certificate2 lbCert,
            GatewayConfig config,
            GatewayRouter router)
        {
            _sessionId = sessionId;
            _clientTcp = clientTcp;
            _lbCert = lbCert;
            _config = config;
            _router = router;
        }

        public async Task RunAsync()
        {
            try
            {
                _clientStream = new SslStream(_clientTcp.GetStream(), false);
                _clientStream.AuthenticateAsServer(_lbCert, false, SslProtocols.Tls12, false);
                Log("Client TLS established.");

                while (!_closed)
                {
                    Packet packet;
                    try
                    {
                        Log("Waiting for client packet.");
                        packet = PacketHelper.Receive(_clientStream);
                    }
                    catch (IOException ioEx)
                    {
                        Log("Client packet receive ended: " + ioEx.Message);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log("Client packet receive failed: " + ex.GetType().Name + ": " + ex.Message);
                        break;
                    }

                    if (packet == null) break;
                    Log($"Received client packet: {packet.Type} requestId={packet.RequestId}");
                    string docId = TryExtractDocId(packet);

                    bool isLeaveDoc = packet.Type == MessageType.DOC_LEAVE && !string.IsNullOrWhiteSpace(docId);
                    bool isSessionPacket = IsDocSessionPacket(packet.Type);

                    if (!string.IsNullOrWhiteSpace(docId) &&
                        !string.IsNullOrWhiteSpace(_activeDocId) &&
                        !string.Equals(_activeDocId, docId, StringComparison.Ordinal))
                    {
                        ReplyError(packet, "Một connection chỉ hỗ trợ 1 tài liệu đang active. Hãy DOC_LEAVE trước.");
                        continue;
                    }

                    int targetBackend = DetermineBackend(packet, docId);
                    if (targetBackend < 0)
                    {
                        continue;
                    }

                    if (!EnsureUpstream(targetBackend))
                    {
                        ReplyError(packet, "Không thể kết nối backend.");
                        continue;
                    }

                    bool startsDocSession = isSessionPacket &&
                        !string.IsNullOrWhiteSpace(docId) &&
                        string.IsNullOrWhiteSpace(_activeDocId);

                    try
                    {
                        Log($"Forwarding {packet.Type}" +
                            (string.IsNullOrWhiteSpace(docId) ? "" : $" doc={docId}") +
                            $" -> backend#{targetBackend}");

                        lock (_upstreamSendLock)
                        {
                            if (_closed || _upstreamStream == null)
                            {
                                throw new IOException("Upstream stream is closed.");
                            }

                            PacketHelper.Send(_upstreamStream, packet);
                        }
                    }
                    catch (Exception sendEx)
                    {
                        if (!_closed)
                        {
                            Log("Upstream send failed: " + sendEx.Message);
                        }
                        ReplyError(packet, "Không gửi được request đến backend.");
                        CloseUpstream();
                        continue;
                    }

                    if (startsDocSession)
                    {
                        _activeDocId = docId;
                        _activeDocBackendIndex = targetBackend;
                        _router.MarkDocSessionStarted(docId, targetBackend);
                        Log($"Activated doc stream: {docId} -> backend#{targetBackend}");
                    }

                    if (isLeaveDoc &&
                        !string.IsNullOrWhiteSpace(_activeDocId) &&
                        string.Equals(_activeDocId, docId, StringComparison.Ordinal))
                    {
                        _router.MarkDocSessionEnded(_activeDocId, _activeDocBackendIndex);
                        Log($"Doc stream ended: {_activeDocId}");
                        _activeDocId = null;
                        _activeDocBackendIndex = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Session loop error: " + ex.Message);
            }
            finally
            {
                Shutdown();
            }

            await Task.CompletedTask;
        }

        private int DetermineBackend(Packet packet, string docId)
        {
            if (!string.IsNullOrWhiteSpace(docId))
            {
                var decision = _router.ResolveDocBackend(docId);
                if (!decision.Success)
                {
                    ReplyError(packet, decision.Error ?? "DOC routing thất bại.");
                    return -1;
                }
                return decision.BackendIndex;
            }

            if (_currentBackendIndex >= 0)
            {
                return _currentBackendIndex;
            }

            int selected = _router.SelectLeastConnectionsBackend();
            if (selected < 0)
            {
                ReplyError(packet, "Không có backend healthy.");
            }
            return selected;
        }

        private bool EnsureUpstream(int backendIndex)
        {
            lock (_upstreamSwitchLock)
            {
                if (_currentBackendIndex == backendIndex &&
                    _upstreamTcp != null &&
                    _upstreamTcp.Connected &&
                    _upstreamStream != null)
                {
                    return true;
                }

                CloseUpstream();

                var node = _router.GetNode(backendIndex);
                if (node == null) return false;

                try
                {
                    var tcp = new TcpClient();
                    var connectTask = tcp.ConnectAsync(node.Endpoint.Host, node.Endpoint.Port);
                    if (!connectTask.Wait(_config.UpstreamConnectTimeoutMs))
                    {
                        try { tcp.Close(); } catch { }
                        _router.MarkBackendHealth(backendIndex, false);
                        return false;
                    }

                    Stream upstream;
                    if (_config.UpstreamTls)
                    {
                        var ssl = new SslStream(
                            tcp.GetStream(),
                            false,
                            (sender, cert, chain, errors) => true);
                        Log($"Backend TLS handshake start (#{backendIndex}) {node.Endpoint}");
                        using (var timeout = new Timer(_ =>
                        {
                            try { tcp.Close(); } catch { }
                        }, null, _config.UpstreamConnectTimeoutMs, Timeout.Infinite))
                        {
                            ssl.AuthenticateAsClient(node.Endpoint.Host, null, SslProtocols.Tls12, false);
                            timeout.Change(Timeout.Infinite, Timeout.Infinite);
                        }
                        Log($"Backend TLS handshake established (#{backendIndex}) {node.Endpoint}");
                        upstream = ssl;
                    }
                    else
                    {
                        upstream = tcp.GetStream();
                    }

                    _upstreamTcp = tcp;
                    _upstreamStream = upstream;
                    _currentBackendIndex = backendIndex;
                    _upstreamCts = new CancellationTokenSource();

                    var upstreamForPump = upstream;
                    var tokenForPump = _upstreamCts.Token;
                    _upstreamPumpTask = Task.Run(() => PumpUpstreamToClient(upstreamForPump, tokenForPump));
                    node.IncrementConnections();
                    _router.MarkBackendHealth(backendIndex, true);
                    Log($"Bound to backend#{backendIndex} {node.Endpoint}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log($"Backend connect failed (#{backendIndex}): {ex.Message}");
                    _router.MarkBackendHealth(backendIndex, false);
                    CloseUpstream();
                    return false;
                }
            }
        }

        private void PumpUpstreamToClient(Stream upstream, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && !_closed)
                {
                    Packet packet = PacketHelper.Receive(upstream);
                    if (packet == null) break;
                    lock (_clientSendLock)
                    {
                        if (_clientStream == null) break;
                        PacketHelper.Send(_clientStream, packet);
                    }
                }
            }
            catch (IOException)
            {
                // backend disconnected
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested && !_closed)
                {
                    Log("Upstream pump error: " + ex.Message);
                }
            }

            if (!_closed && !token.IsCancellationRequested)
            {
                Shutdown();
            }
        }

        private void ReplyError(Packet origin, string message)
        {
            if (origin == null || _clientStream == null) return;

            var errorPacket = Packet.Create(MessageType.ERROR, new Payload_ERROR
            {
                Message = string.IsNullOrWhiteSpace(message) ? "Gateway routing error." : message
            });
            errorPacket.RequestId = origin.RequestId;

            try
            {
                lock (_clientSendLock)
                {
                    PacketHelper.Send(_clientStream, errorPacket);
                }
            }
            catch
            {
                Shutdown();
            }
        }

        private void CloseUpstream()
        {
            lock (_upstreamSwitchLock)
            {
                try { _upstreamCts?.Cancel(); } catch { }

                if (_currentBackendIndex >= 0)
                {
                    var node = _router.GetNode(_currentBackendIndex);
                    node?.DecrementConnections();
                }

                lock (_upstreamSendLock)
                {
                    try { _upstreamStream?.Close(); } catch { }
                    try { _upstreamTcp?.Close(); } catch { }
                }
                _upstreamStream = null;
                _upstreamTcp = null;
                _upstreamCts = null;
                _upstreamPumpTask = null;
                _currentBackendIndex = -1;
            }
        }

        private void Shutdown()
        {
            if (_closed) return;
            _closed = true;

            if (!string.IsNullOrWhiteSpace(_activeDocId) && _activeDocBackendIndex >= 0)
            {
                _router.MarkDocSessionEnded(_activeDocId, _activeDocBackendIndex);
            }
            _activeDocId = null;
            _activeDocBackendIndex = -1;

            CloseUpstream();
            try { _clientStream?.Close(); } catch { }
            try { _clientTcp?.Close(); } catch { }
            Log("Session closed.");
        }

        private static string TryExtractDocId(Packet packet)
        {
            if (packet == null || string.IsNullOrWhiteSpace(packet.Payload))
            {
                return null;
            }

            try
            {
                var token = JToken.Parse(packet.Payload);
                if (token.Type != JTokenType.Object) return null;

                var obj = (JObject)token;
                string docId = GetString(obj, "docID") ?? GetString(obj, "docId");
                if (!string.IsNullOrWhiteSpace(docId))
                {
                    return docId.Trim();
                }

                var nestedComment = obj["comment"] as JObject;
                if (nestedComment != null)
                {
                    docId = GetString(nestedComment, "docID") ?? GetString(nestedComment, "docId");
                    if (!string.IsNullOrWhiteSpace(docId))
                    {
                        return docId.Trim();
                    }
                }
            }
            catch
            {
                // ignore parse errors, packet có thể không chứa docID
            }

            return null;
        }

        private static string GetString(JObject obj, string name)
        {
            if (obj == null || string.IsNullOrWhiteSpace(name)) return null;

            foreach (var prop in obj.Properties())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Value?.Type == JTokenType.Null ? null : prop.Value?.ToString();
                }
            }
            return null;
        }

        private static readonly HashSet<MessageType> SessionScopedTypes = new HashSet<MessageType>
        {
            MessageType.DOC_OPEN,
            MessageType.OP_INSERT,
            MessageType.OP_DELETE
        };

        private static bool IsDocSessionPacket(MessageType type)
        {
            return SessionScopedTypes.Contains(type);
        }

        private void Log(string message)
        {
            Console.WriteLine($"[{DateTime.UtcNow:O}] [LB] [session:{_sessionId}] {message}");
        }
    }
}
