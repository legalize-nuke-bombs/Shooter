using System.Collections.Generic;

namespace Shooter.Server
{
    public class ServerSessionGrants
    {
        private class Entry
        {
            public string WorldId { get; }
            public long ExpiresAt { get; }

            public Entry(string worldId, long expiresAt)
            {
                WorldId = worldId;
                ExpiresAt = expiresAt;
            }
        }

        private readonly Dictionary<long, Entry> grants = new Dictionary<long, Entry>();

        public void Open(long userId, string worldId, long expiresAt)
        {
            grants[userId] = new Entry(worldId, expiresAt);
        }

        public bool TryConsume(long userId, long now, out string worldId)
        {
            worldId = null;
            if (!grants.TryGetValue(userId, out Entry entry)) return false;
            grants.Remove(userId);
            if (entry.ExpiresAt < now) return false;
            worldId = entry.WorldId;
            return true;
        }

        public void Close(long userId, string worldId)
        {
            if (grants.TryGetValue(userId, out Entry entry) && entry.WorldId == worldId)
                grants.Remove(userId);
        }

        public void CloseWorld(string worldId)
        {
            var closed = new List<long>();
            foreach (KeyValuePair<long, Entry> pair in grants)
                if (pair.Value.WorldId == worldId)
                    closed.Add(pair.Key);
            foreach (long userId in closed)
                grants.Remove(userId);
        }

        public int Sweep(long now)
        {
            var expired = new List<long>();
            foreach (KeyValuePair<long, Entry> pair in grants)
                if (pair.Value.ExpiresAt < now)
                    expired.Add(pair.Key);
            foreach (long userId in expired)
                grants.Remove(userId);
            return expired.Count;
        }
    }
}
