using System.Collections.Generic;

namespace Shooter.GameServer
{
    public class BanList
    {
        private readonly Dictionary<string, long> pairBans = new Dictionary<string, long>();
        private readonly Dictionary<long, long> userBans = new Dictionary<long, long>();
        private readonly Dictionary<string, long> worldBans = new Dictionary<string, long>();

        public void BanPair(long userId, string worldId, long since)
        {
            pairBans[userId + ":" + worldId] = since;
        }

        public void BanUser(long userId, long since)
        {
            userBans[userId] = since;
        }

        public void BanWorld(string worldId, long since)
        {
            worldBans[worldId] = since;
        }

        public bool IsBanned(long userId, string worldId, long tokenIat)
        {
            if (pairBans.TryGetValue(userId + ":" + worldId, out long banTime) && tokenIat < banTime) return true;
            if (userBans.TryGetValue(userId, out banTime) && tokenIat < banTime) return true;
            if (worldBans.TryGetValue(worldId, out banTime) && tokenIat < banTime) return true;
            return false;
        }

        public int Sweep(long cutoff)
        {
            return SweepMap(pairBans, cutoff) + SweepMap(userBans, cutoff) + SweepMap(worldBans, cutoff);
        }

        private static int SweepMap<TKey>(Dictionary<TKey, long> bans, long cutoff)
        {
            var expired = new List<TKey>();
            foreach (KeyValuePair<TKey, long> pair in bans)
                if (pair.Value < cutoff)
                    expired.Add(pair.Key);
            foreach (TKey key in expired)
                bans.Remove(key);
            return expired.Count;
        }
    }
}
