using System;
using System.Collections.Generic;
using Shooter.Auth;
using Shooter.Logging;
using Shooter.Serialization;

namespace Shooter.Server.Sessions
{
    public class ServerSessionGate
    {
        private const long AllowTtlSeconds = 60;
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

        public bool AuthorizeHook(string token)
        {
            return Jwt.TryVerify(token, jwtSecret, out string subject) && subject == "hook";
        }

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

        public IReadOnlyList<int> HandleHook(string json)
        {
            SessionHook hook = Json.Deserialize<SessionHook>(json);
            if (hook == null || string.IsNullOrEmpty(hook.WorldId))
            {
                Log.Warn("Malformed hook, ignoring");
                return Array.Empty<int>();
            }

            switch (hook.Action)
            {
                case SessionHookAction.OpenSession:
                    if (hook.UserId == null)
                    {
                        Log.Warn("Hook {} for world {} has no user, ignoring", hook.Action, hook.WorldId);
                        return Array.Empty<int>();
                    }
                    serverSessionGrants.Open(hook.UserId.Value, hook.WorldId, hook.DisplayName, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + AllowTtlSeconds);
                    Log.Info("Session opened: user {} '{}' world {}", hook.UserId.Value, hook.DisplayName, hook.WorldId);
                    return Array.Empty<int>();
                case SessionHookAction.CloseSession:
                    return CloseSessions(hook.UserId, hook.WorldId);
                default:
                    Log.Warn("Unknown hook action {}, ignoring", hook.Action);
                    return Array.Empty<int>();
            }
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

        private IReadOnlyList<int> CloseSessions(long? userId, string worldId)
        {
            bool wholeWorld = userId == null;
            if (wholeWorld) serverSessionGrants.CloseWorld(worldId);
            else serverSessionGrants.Close(userId.Value, worldId);

            var toKick = new List<int>();
            foreach (ServerSession session in sessions.Values)
                if (session.WorldId == worldId && (wholeWorld || session.UserId == userId.Value))
                    toKick.Add(session.ConnId);

            Log.Info("Session closed: user {} world {}, kicking online {}", wholeWorld ? "*" : userId.Value.ToString(), worldId, toKick.Count);
            return toKick;
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
