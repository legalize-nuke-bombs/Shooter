using System.Collections.Generic;

namespace Shooter.GameServer
{
    public class AllowList
    {
        private class Entry
        {
            public string WorldId;
            public long ExpiresAt;
        }

        private readonly Dictionary<long, Entry> allows = new Dictionary<long, Entry>();

        public void Open(long userId, string worldId, long expiresAt)
        {
            allows[userId] = new Entry { WorldId = worldId, ExpiresAt = expiresAt };
        }

        public bool TryConsume(long userId, long now, out string worldId)
        {
            worldId = null;
            if (!allows.TryGetValue(userId, out Entry entry)) return false;
            allows.Remove(userId);
            if (entry.ExpiresAt < now) return false;
            worldId = entry.WorldId;
            return true;
        }

        public void Close(long userId, string worldId)
        {
            if (allows.TryGetValue(userId, out Entry entry) && entry.WorldId == worldId)
                allows.Remove(userId);
        }

        public void CloseWorld(string worldId)
        {
            var closed = new List<long>();
            foreach (KeyValuePair<long, Entry> pair in allows)
                if (pair.Value.WorldId == worldId)
                    closed.Add(pair.Key);
            foreach (long userId in closed)
                allows.Remove(userId);
        }

        public int Sweep(long now)
        {
            var expired = new List<long>();
            foreach (KeyValuePair<long, Entry> pair in allows)
                if (pair.Value.ExpiresAt < now)
                    expired.Add(pair.Key);
            foreach (long userId in expired)
                allows.Remove(userId);
            return expired.Count;
        }
    }
}
