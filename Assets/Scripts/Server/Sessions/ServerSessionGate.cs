using System;
using System.Collections.Generic;
using Shooter.Auth;
using Shooter.Logging;

namespace Shooter.Server.Sessions
{
    public class ServerSessionGate
    {
        private const float SweepInterval = 60f;

        private readonly byte[] jwtSecret;
        private readonly ServerSessionGrants serverSessionGrants = new ServerSessionGrants();
        private readonly Dictionary<int, ServerSession> sessions = new Dictionary<int, ServerSession>();
        private float sweepTimer;

        public ServerSessionGate(byte[] jwtSecret)
        {
            this.jwtSecret = jwtSecret;
        }

        public int Count => sessions.Count;

        public bool TryAdmit(int connId, string query, out ServerSession session)
        {
            session = null;

            string token = ExtractQueryParam(query, "token");
            if (!Jwt.TryVerify(token, jwtSecret, out string subject))
            {
                Log.Warn("Conn {} token rejected", connId);
                return false;
            }
            if (!long.TryParse(subject, out long userId))
            {
                Log.Warn("Conn {} not a user token (sub '{}')", connId, subject);
                return false;
            }
            if (!serverSessionGrants.TryConsume(userId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), out SessionGrant grant))
            {
                Log.Warn("Conn {} user {} has no open session", connId, userId);
                return false;
            }

            session = new ServerSession(connId, userId, grant.WorldId, grant.DisplayName);
            sessions[connId] = session;
            Log.Info("Conn {} authed: user {} '{}' world {}", connId, userId, session.DisplayName, grant.WorldId);
            return true;
        }

        public bool TryGet(int connId, out ServerSession session)
        {
            return sessions.TryGetValue(connId, out session);
        }

        public void Remove(int connId)
        {
            if (!sessions.Remove(connId)) return;

            Log.Info("Conn {} session removed, sessions total {}", connId, sessions.Count);
        }

        public IEnumerable<int> ConnIdsInWorld(string worldId)
        {
            foreach (ServerSession session in sessions.Values)
                if (session.InWorld && session.WorldId == worldId)
                    yield return session.ConnId;
        }

        public IReadOnlyList<int> ConnIdsOf(long? userId, string worldId)
        {
            var found = new List<int>();
            foreach (ServerSession session in sessions.Values)
                if (session.WorldId == worldId && (userId == null || session.UserId == userId.Value))
                    found.Add(session.ConnId);

            return found;
        }

        public void OpenGrant(long userId, string worldId, string displayName, long expiresAt)
        {
            serverSessionGrants.Open(userId, worldId, displayName, expiresAt);
        }

        public void CloseGrant(long? userId, string worldId)
        {
            if (userId == null) serverSessionGrants.CloseWorld(worldId);
            else serverSessionGrants.Close(userId.Value, worldId);
        }

        public void Tick(float dt)
        {
            sweepTimer += dt;
            if (sweepTimer < SweepInterval) return;

            sweepTimer -= SweepInterval;
            int swept = serverSessionGrants.Sweep(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            if (swept > 0)
                Log.Info("Swept {} expired session grants", swept);
        }

        private static string ExtractQueryParam(string query, string name)
        {
            foreach (string pair in query.Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq <= 0) continue;
                if (pair.Substring(0, eq) == name)
                    return Uri.UnescapeDataString(pair.Substring(eq + 1));
            }
            return "";
        }
    }
}
