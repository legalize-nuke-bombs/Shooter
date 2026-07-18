using System;
using System.Collections.Generic;
using Shooter.Auth;
using Shooter.Serialization;
using Shooter.Server.Entities.Players;
using Shooter.Logging;

namespace Shooter.Server.Sessions
{
    public class ServerSessionGate
    {
        private const long AllowTtlSeconds = 60;
        private const float SweepInterval = 60f;

        private readonly byte[] jwtSecret;
        private readonly ServerSessionGrants serverSessionGrants = new ServerSessionGrants();
        private readonly Dictionary<int, Player> sessions = new Dictionary<int, Player>();
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

        public bool TryAdmit(int connId, string query, out Player player)
        {
            player = null;

            string token = ExtractQueryParam(query, "token");
            if (!Jwt.TryVerify(token, jwtSecret, out string subject))
            {
                Log.Warn("conn " + connId + " token rejected");
                return false;
            }
            if (!long.TryParse(subject, out long userId))
            {
                Log.Warn("conn " + connId + " not a user token (sub '" + subject + "')");
                return false;
            }
            if (!serverSessionGrants.TryConsume(userId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), out string worldId))
            {
                Log.Warn("conn " + connId + " user " + userId + " has no open session");
                return false;
            }

            player = new Player(connId, userId, worldId);
            sessions[connId] = player;
            Log.Info("conn " + connId + " authed: user " + userId + " world " + worldId);
            return true;
        }

        public bool TryGet(int connId, out Player player)
        {
            return sessions.TryGetValue(connId, out player);
        }

        public void Remove(int connId)
        {
            sessions.Remove(connId);
        }

        public IReadOnlyList<int> HandleHook(string json)
        {
            SessionHook hook = Json.Deserialize<SessionHook>(json);
            if (hook == null || string.IsNullOrEmpty(hook.action) || string.IsNullOrEmpty(hook.worldId))
            {
                Log.Warn("malformed hook, ignoring");
                return Array.Empty<int>();
            }

            switch (hook.action)
            {
                case "OPEN_SESSION":
                    serverSessionGrants.Open(hook.userId, hook.worldId, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + AllowTtlSeconds);
                    Log.Info("session opened: user " + hook.userId + " world " + hook.worldId);
                    return Array.Empty<int>();
                case "CLOSE_SESSION":
                    return CloseSessions(hook.userId, hook.worldId);
                default:
                    Log.Warn("unknown hook action " + hook.action + ", ignoring");
                    return Array.Empty<int>();
            }
        }

        private IReadOnlyList<int> CloseSessions(long userId, string worldId)
        {
            bool wholeWorld = userId == 0;
            if (wholeWorld) serverSessionGrants.CloseWorld(worldId);
            else serverSessionGrants.Close(userId, worldId);

            var toKick = new List<int>();
            foreach (Player player in sessions.Values)
                if (player.WorldId == worldId && (wholeWorld || player.UserId == userId))
                    toKick.Add(player.ConnId);

            Log.Info("session closed: user " + (wholeWorld ? "*" : userId.ToString()) + " world " + worldId + ", kicking online " + toKick.Count);
            return toKick;
        }

        public void Tick(float dt)
        {
            sweepTimer += dt;
            if (sweepTimer < SweepInterval) return;
            sweepTimer = 0f;
            int swept = serverSessionGrants.Sweep(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            if (swept > 0)
                Log.Info("swept " + swept + " expired session grants");
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
