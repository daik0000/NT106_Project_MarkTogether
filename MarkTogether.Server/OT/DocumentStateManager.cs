using System.Collections.Concurrent;

namespace MarkTogether.Server.OT
{
    // [OT] Manager for document states
    public static class DocumentStateManager
    {
        private static readonly ConcurrentDictionary<string, DocumentState> _states 
            = new ConcurrentDictionary<string, DocumentState>();

        public static DocumentState GetOrCreate(string docId)
        {
            return _states.GetOrAdd(docId, id => new DocumentState(id));
        }

        public static void Remove(string docId)
        {
            _states.TryRemove(docId, out _);
        }
    }
}
