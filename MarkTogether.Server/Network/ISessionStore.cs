using System;

namespace MarkTogether.Server.Network
{
    public interface ISessionStore : IDisposable
    {
        bool RemoveOnDisconnect { get; }
        void AddOrUpdate(string token, int userId);
        int GetUserId(string token);
        void Remove(string token);
        bool Exists(string token);
    }
}
