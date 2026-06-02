using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MarkTogether.Gateway
{
    internal sealed class RoutingDecision
    {
        public bool Success { get; set; }
        public int BackendIndex { get; set; } = -1;
        public string Error { get; set; }
    }

    internal sealed class BackendNode
    {
        private int _activeConnections;
        private int _isHealthy = 1;

        public BackendNode(BackendEndpoint endpoint)
        {
            Endpoint = endpoint;
        }

        public BackendEndpoint Endpoint { get; }

        public int ActiveConnections => Volatile.Read(ref _activeConnections);

        public bool IsHealthy
        {
            get => Volatile.Read(ref _isHealthy) == 1;
            set => Interlocked.Exchange(ref _isHealthy, value ? 1 : 0);
        }

        public void IncrementConnections() => Interlocked.Increment(ref _activeConnections);

        public void DecrementConnections()
        {
            int value;
            do
            {
                value = Volatile.Read(ref _activeConnections);
                if (value <= 0) return;
            } while (Interlocked.CompareExchange(ref _activeConnections, value - 1, value) != value);
        }
    }

    internal sealed class DocRouteEntry
    {
        public string DocId { get; set; }
        public int BackendIndex { get; set; }
        public int ActiveStreams { get; set; }
        public DateTime LastSeenUtc { get; set; }
    }

    internal sealed class GatewayRouter
    {
        private readonly TimeSpan _docRemapTimeout;
        private readonly List<BackendNode> _nodes;
        private readonly ConcurrentDictionary<string, DocRouteEntry> _docRoutes
            = new ConcurrentDictionary<string, DocRouteEntry>(StringComparer.Ordinal);
        private readonly object _docLock = new object();

        public GatewayRouter(IEnumerable<BackendEndpoint> endpoints, TimeSpan docRemapTimeout)
        {
            _docRemapTimeout = docRemapTimeout;
            _nodes = endpoints.Select(e => new BackendNode(e)).ToList();
        }

        public IReadOnlyList<BackendNode> Nodes => _nodes;

        public BackendNode GetNode(int index)
        {
            if (index < 0 || index >= _nodes.Count) return null;
            return _nodes[index];
        }

        public int SelectLeastConnectionsBackend()
        {
            var candidates = _nodes
                .Select((n, idx) => new { Node = n, Index = idx })
                .Where(x => x.Node.IsHealthy)
                .ToList();

            if (candidates.Count == 0)
            {
                return -1;
            }

            int minConn = candidates.Min(x => x.Node.ActiveConnections);
            var best = candidates.Where(x => x.Node.ActiveConnections == minConn).ToList();

            if (best.Count == 1)
            {
                return best[0].Index;
            }

            // Tie-break deterministic theo thời gian để tránh bias backend đầu danh sách.
            int tieIndex = Math.Abs(Environment.TickCount) % best.Count;
            return best[tieIndex].Index;
        }

        public RoutingDecision ResolveDocBackend(string docId)
        {
            if (string.IsNullOrWhiteSpace(docId))
            {
                return new RoutingDecision { Success = false, Error = "docID không hợp lệ." };
            }

            lock (_docLock)
            {
                if (_docRoutes.TryGetValue(docId, out var existing)) // _______________
                {
                    var node = GetNode(existing.BackendIndex);
                    if (node != null && node.IsHealthy) // nếu đã có map 
                    {
                        return new RoutingDecision { Success = true, BackendIndex = existing.BackendIndex };
                    }

                    bool canRemap = existing.ActiveStreams <= 0
                        && (DateTime.UtcNow - existing.LastSeenUtc) >= _docRemapTimeout;
                    if (!canRemap)
                    {
                        return new RoutingDecision
                        {
                            Success = false,
                            Error = $"DOC '{docId}' tạm thời chưa thể remap. Vui lòng thử lại sau."
                        };
                    }

                    int remapped = SelectRendezvousBackend(docId); // map nếu chưa có map 
                    if (remapped < 0)
                    {
                        return new RoutingDecision
                        {
                            Success = false,
                            Error = "Không có backend healthy để xử lý tài liệu."
                        };
                    }

                    existing.BackendIndex = remapped;
                    existing.LastSeenUtc = DateTime.UtcNow;
                    existing.ActiveStreams = 0;
                    return new RoutingDecision { Success = true, BackendIndex = remapped };
                }

                int selected = SelectRendezvousBackend(docId);
                if (selected < 0)
                {
                    return new RoutingDecision
                    {
                        Success = false,
                        Error = "Không có backend healthy để xử lý tài liệu."
                    };
                }

                _docRoutes[docId] = new DocRouteEntry
                {
                    DocId = docId,
                    BackendIndex = selected,
                    ActiveStreams = 0,
                    LastSeenUtc = DateTime.UtcNow
                };

                return new RoutingDecision { Success = true, BackendIndex = selected };
            }
        }

        public void MarkDocSessionStarted(string docId, int backendIndex)
        {
            if (string.IsNullOrWhiteSpace(docId) || backendIndex < 0) return;

            lock (_docLock)
            {
                var entry = _docRoutes.GetOrAdd(docId, _ => new DocRouteEntry
                {
                    DocId = docId,
                    BackendIndex = backendIndex,
                    ActiveStreams = 0,
                    LastSeenUtc = DateTime.UtcNow
                });

                entry.BackendIndex = backendIndex;
                entry.ActiveStreams++;
                entry.LastSeenUtc = DateTime.UtcNow;
            }
        }

        public void MarkDocSessionEnded(string docId, int backendIndex)
        {
            if (string.IsNullOrWhiteSpace(docId) || backendIndex < 0) return;

            lock (_docLock)
            {
                if (_docRoutes.TryGetValue(docId, out var entry))
                {
                    if (entry.BackendIndex == backendIndex && entry.ActiveStreams > 0)
                    {
                        entry.ActiveStreams--;
                    }
                    entry.LastSeenUtc = DateTime.UtcNow;
                }
            }
        }

        public void MarkBackendHealth(int backendIndex, bool healthy)
        {
            var node = GetNode(backendIndex);
            if (node == null) return;
            node.IsHealthy = healthy;
        }

        private int SelectRendezvousBackend(string docId)
        {
            long bestScore = long.MinValue;
            int bestIndex = -1;

            for (int i = 0; i < _nodes.Count; i++)
            {
                if (!_nodes[i].IsHealthy) continue;

                long score = ComputeScore(docId, _nodes[i].Endpoint.ToString());
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static long ComputeScore(string docId, string backendId)
        {
            string input = docId + "|" + backendId;
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                return BitConverter.ToInt64(hash, 0);
            }
        }
    }
}
