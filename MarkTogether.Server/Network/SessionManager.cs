using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MarkTogether.Shared;

namespace MarkTogether.Server.Network
{
    /// <summary>
    /// Quản lý:
    ///  - Session: token ↔ userId
    ///  - DocRoom: docId ↔ Set<ClientHandler> đang mở doc
    /// Thread-safe.
    /// </summary>
    public static class SessionManager
    {
        // ─── Session token ↔ userId ─────────────────────────
        private static readonly ConcurrentDictionary<string, int> _sessions
            = new ConcurrentDictionary<string, int>();

        // ─── docId ↔ Set<ClientHandler> ─────────────────────
        private static readonly ConcurrentDictionary<string, HashSet<ClientHandler>> _docRooms
            = new ConcurrentDictionary<string, HashSet<ClientHandler>>();

        // Lock cho HashSet bên trong (ConcurrentDictionary chỉ thread-safe ở mức key/value)
        private static readonly object _roomsLock = new object();

        // ═══════════════════════════════════════════════════════
        //  SESSION
        // ═══════════════════════════════════════════════════════
        public static void AddSession(string token, int userId)
        {
            if (!string.IsNullOrEmpty(token))
                _sessions[token] = userId;
        }

        public static int GetUserId(string token)
        {
            if (string.IsNullOrEmpty(token)) return -1;
            return _sessions.TryGetValue(token, out int userId) ? userId : -1;
        }

        public static void RemoveSession(string token)
        {
            if (!string.IsNullOrEmpty(token))
                _sessions.TryRemove(token, out _);
        }

        public static bool IsValid(string token)
        {
            return !string.IsNullOrEmpty(token) && _sessions.ContainsKey(token);
        }

        // ═══════════════════════════════════════════════════════
        //  DOC ROOM
        // ═══════════════════════════════════════════════════════
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

        /// <summary>
        /// Khi client disconnect — xoá khỏi mọi room.
        /// </summary>
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

        /// <summary>
        /// Broadcast packet tới mọi client trong room. Có thể loại trừ 1 client.
        /// </summary>
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