using System.Collections.Concurrent;

namespace MarkTogether.Server.Network
{
    public sealed class InMemorySessionStore : ISessionStore
    {
        public bool RemoveOnDisconnect => true;

        private readonly ConcurrentDictionary<string, int> _sessions
            = new ConcurrentDictionary<string, int>();

        public void AddOrUpdate(string token, int userId)
        {
            if (string.IsNullOrEmpty(token)) return;
            _sessions[token] = userId;
        }

        public int GetUserId(string token)
        {
            if (string.IsNullOrEmpty(token)) return -1;
            return _sessions.TryGetValue(token, out int userId) ? userId : -1;
        }

        public void Remove(string token)
        {
            if (string.IsNullOrEmpty(token)) return;
            _sessions.TryRemove(token, out _);
        }

        public bool Exists(string token)
        {
            return !string.IsNullOrEmpty(token) && _sessions.ContainsKey(token);
        }

        public void Dispose()
        {
            // No-op
        }
    }
}
