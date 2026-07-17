using System;
using System.Collections.Generic;
using Shooter.Auth;
using Shooter.Net;
using Shooter.Entities.Player;
using Shooter.Logging;

namespace Shooter.Server
{
    public class SessionGate
    {
        private const long AllowTtlSeconds = 60;

        private readonly byte[] jwtSecret;
        private readonly AllowList allows = new AllowList();
        private readonly Dictionary<int, ServerPlayer> sessions = new Dictionary<int, ServerPlayer>();

        public SessionGate(byte[] jwtSecret)
        {
            this.jwtSecret = jwtSecret;
        }

        public int Count => sessions.Count;

        public bool AuthorizeHook(string token)
        {
            return Jwt.TryVerify(token, jwtSecret, out string subject) && subject == "hook";
        }

        public bool TryAdmit(int connId, string query, out ServerPlayer player)
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
            if (!allows.TryConsume(userId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), out string worldId))
            {
                Log.Warn("conn " + connId + " user " + userId + " has no open session");
                return false;
            }

            player = new ServerPlayer
            {
                ConnId = connId,
                UserId = userId,
                DisplayName = "player" + userId,
                WorldId = worldId
            };
            sessions[connId] = player;
            Log.Info("conn " + connId + " authed: user " + userId + " world " + worldId);
            return true;
        }

        public bool TryGet(int connId, out ServerPlayer player)
        {
            return sessions.TryGetValue(connId, out player);
        }

        public void Remove(int connId)
        {
            sessions.Remove(connId);
        }

        public IReadOnlyList<int> HandleHook(string json)
        {
            UnityHookMsg hook = NetJson.Parse<UnityHookMsg>(json);
            if (hook == null || string.IsNullOrEmpty(hook.action) || string.IsNullOrEmpty(hook.worldId))
            {
                Log.Warn("malformed hook, ignoring");
                return Array.Empty<int>();
            }

            switch (hook.action)
            {
                case "OPEN_SESSION":
                    allows.Open(hook.userId, hook.worldId, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + AllowTtlSeconds);
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
            if (wholeWorld) allows.CloseWorld(worldId);
            else allows.Close(userId, worldId);

            var toKick = new List<int>();
            foreach (ServerPlayer p in sessions.Values)
                if (p.WorldId == worldId && (wholeWorld || p.UserId == userId))
                    toKick.Add(p.ConnId);

            Log.Info("session closed: user " + (wholeWorld ? "*" : userId.ToString()) + " world " + worldId + ", kicking online " + toKick.Count);
            return toKick;
        }

        public int Sweep()
        {
            return allows.Sweep(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
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
