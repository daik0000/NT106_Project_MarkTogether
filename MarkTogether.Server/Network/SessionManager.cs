using System.Collections.Concurrent;

namespace MarkTogether.Server.Network
{
    /// <summary>
    /// Quản lý session token ↔ userId (in-memory).
    /// Thread-safe nhờ ConcurrentDictionary.
    /// </summary>
    public static class SessionManager
    {
        // Key = Token (string), Value = UserId (int)
        private static readonly ConcurrentDictionary<string, int> _sessions
            = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// Thêm session mới sau khi login/register thành công.
        /// </summary>
        public static void AddSession(string token, int userId)
        {
            _sessions[token] = userId;
        }

        /// <summary>
        /// Lấy userId từ token. Trả -1 nếu token không hợp lệ.
        /// </summary>
        public static int GetUserId(string token)
        {
            if (string.IsNullOrEmpty(token)) return -1;
            return _sessions.TryGetValue(token, out int userId) ? userId : -1;
        }

        /// <summary>
        /// Xóa session (logout hoặc disconnect).
        /// </summary>
        public static void RemoveSession(string token)
        {
            if (!string.IsNullOrEmpty(token))
                _sessions.TryRemove(token, out _);
        }

        /// <summary>
        /// Kiểm tra token có hợp lệ không.
        /// </summary>
        public static bool IsValid(string token)
        {
            return !string.IsNullOrEmpty(token) && _sessions.ContainsKey(token);
        }
    }
}
