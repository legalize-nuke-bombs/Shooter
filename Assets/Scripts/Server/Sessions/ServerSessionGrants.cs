using System.Collections.Generic;

namespace Shooter.Server.Sessions
{
    public class ServerSessionGrants
    {
        private readonly Dictionary<long, SessionGrant> grants = new Dictionary<long, SessionGrant>();

        public void Open(long userId, string worldId, string displayName, long expiresAt)
        {
            grants[userId] = new SessionGrant(worldId, displayName, expiresAt);
        }

        public bool TryConsume(long userId, long now, out SessionGrant grant)
        {
            if (!grants.TryGetValue(userId, out grant)) return false;

            grants.Remove(userId);
            if (grant.ExpiresAt >= now) return true;

            grant = null;
            return false;
        }

        public void Close(long userId, string worldId)
        {
            if (grants.TryGetValue(userId, out SessionGrant grant) && grant.WorldId == worldId)
                grants.Remove(userId);
        }

        public void CloseWorld(string worldId)
        {
            var closed = new List<long>();
            foreach (KeyValuePair<long, SessionGrant> pair in grants)
                if (pair.Value.WorldId == worldId)
                    closed.Add(pair.Key);

            foreach (long userId in closed)
                grants.Remove(userId);
        }

        public int Sweep(long now)
        {
            var expired = new List<long>();
            foreach (KeyValuePair<long, SessionGrant> pair in grants)
                if (pair.Value.ExpiresAt < now)
                    expired.Add(pair.Key);

            foreach (long userId in expired)
                grants.Remove(userId);

            return expired.Count;
        }
    }
}
