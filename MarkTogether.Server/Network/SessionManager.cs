using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MarkTogether.Shared;

namespace MarkTogether.Server.Network
{
    /// <summary>
    /// Quản lý:
    ///  - Session: token ↔ userId (Redis hoặc in-memory fallback)
    ///  - DocRoom: docId ↔ tập ClientHandler đang mở doc
    /// Thread-safe.
    /// </summary>
    public static class SessionManager
    {
        private static readonly ISessionStore _sessionStore = SessionStoreFactory.Create();

        private static readonly ConcurrentDictionary<string, HashSet<ClientHandler>> _docRooms
            = new ConcurrentDictionary<string, HashSet<ClientHandler>>();

        private static readonly object _roomsLock = new object();
        public static bool RemoveSessionOnDisconnect => _sessionStore.RemoveOnDisconnect;

        public static void AddSession(string token, int userId)
        {
            _sessionStore.AddOrUpdate(token, userId);
        }

        public static int GetUserId(string token)
        {
            return _sessionStore.GetUserId(token);
        }

        public static void RemoveSession(string token)
        {
            _sessionStore.Remove(token);
        }

        public static bool IsValid(string token)
        {
            return _sessionStore.Exists(token);
        }

        public static void JoinRoom(string docId, ClientHandler handler)
        {
            if (string.IsNullOrEmpty(docId) || handler == null) return;

            lock (_roomsLock)
            {
                if (!_docRooms.TryGetValue(docId, out var set))
                {
                    set = new HashSet<ClientHandler>();
                    _docRooms[docId] = set;
                }
                set.Add(handler);
            }
        }

        public static void LeaveRoom(string docId, ClientHandler handler)
        {
            if (string.IsNullOrEmpty(docId) || handler == null) return;

            lock (_roomsLock)
            {
                if (_docRooms.TryGetValue(docId, out var set))
                {
                    set.Remove(handler);
                    if (set.Count == 0)
                    {
                        _docRooms.TryRemove(docId, out _);
                    }
                }
            }
        }

        public static void LeaveAllRooms(ClientHandler handler)
        {
            if (handler == null) return;

            lock (_roomsLock)
            {
                var emptyRooms = new List<string>();
                foreach (var kv in _docRooms)
                {
                    if (kv.Value.Remove(handler) && kv.Value.Count == 0)
                    {
                        emptyRooms.Add(kv.Key);
                    }
                }

                foreach (var key in emptyRooms)
                {
                    _docRooms.TryRemove(key, out _);
                }
            }
        }

        public static List<ClientHandler> GetClientsInRoom(string docId)
        {
            if (string.IsNullOrEmpty(docId)) return new List<ClientHandler>();

            lock (_roomsLock)
            {
                if (_docRooms.TryGetValue(docId, out var set))
                {
                    return set.ToList();
                }
                return new List<ClientHandler>();
            }
        }

        public static void BroadcastToRoom(string docId, Packet packet, ClientHandler exclude = null)
        {
            if (string.IsNullOrEmpty(docId) || packet == null) return;

            List<ClientHandler> targets;
            lock (_roomsLock)
            {
                if (!_docRooms.TryGetValue(docId, out var set)) return;
                targets = set.Where(h => h != exclude).ToList();
            }

            foreach (var handler in targets)
            {
                try
                {
                    handler.SendSafe(packet);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SessionManager] Broadcast lỗi tới handler: {ex.Message}");
                }
            }
        }
    }
}
