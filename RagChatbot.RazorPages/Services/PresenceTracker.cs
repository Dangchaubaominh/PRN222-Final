using System.Collections.Concurrent;

namespace RagChatbot.RazorPages.Services
{
    public class PresenceTracker : IPresenceTracker
    {
        // userId -> số kết nối (tab) đang mở của user đó
        private readonly ConcurrentDictionary<string, int> _connections = new();

        public int OnlineCount => _connections.Count;

        public bool Connect(string userId)
        {
            bool becameOnline = false;
            _connections.AddOrUpdate(userId,
                _ => { becameOnline = true; return 1; },
                (_, count) => count + 1);
            return becameOnline;
        }

        public bool Disconnect(string userId)
        {
            if (!_connections.TryGetValue(userId, out int count))
                return false;

            if (count <= 1)
            {
                _connections.TryRemove(userId, out _);
                return true;
            }

            _connections.TryUpdate(userId, count - 1, count);
            return false;
        }
    }
}
