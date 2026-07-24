using System;
using System.Collections.Generic;
using Shooter.Logging;
using Shooter.Serialization;
using Shooter.Server.Sessions;
using Shooter.Server.Transport;

namespace Shooter.Server.Control
{
    public sealed class ServerControlApi
    {
        private const long GrantTtlSeconds = 60;

        private readonly ServerSessionGate serverSessionGate;
        private readonly IServerTransport serverTransport;

        public ServerControlApi(ServerSessionGate serverSessionGate, IServerTransport serverTransport)
        {
            this.serverSessionGate = serverSessionGate;
            this.serverTransport = serverTransport;
        }

        public void Handle(string json)
        {
            SessionHook hook = Json.Deserialize<SessionHook>(json);
            if (hook == null || string.IsNullOrEmpty(hook.WorldId))
            {
                Log.Warn("Malformed hook, ignoring");
                return;
            }

            switch (hook.Action)
            {
                case SessionHookAction.OpenSession:
                    OpenSession(hook);
                    break;
                case SessionHookAction.CloseSession:
                    CloseSession(hook);
                    break;
                default:
                    Log.Warn("Unknown hook action {} for world {}, ignoring", hook.Action, hook.WorldId);
                    break;
            }
        }

        private void OpenSession(SessionHook hook)
        {
            if (hook.UserId == null)
            {
                Log.Warn("Hook {} for world {} has no user, ignoring", hook.Action, hook.WorldId);
                return;
            }

            long expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + GrantTtlSeconds;
            serverSessionGate.OpenGrant(hook.UserId.Value, hook.WorldId, hook.DisplayName, expiresAt);
            Log.Info("Session opened: user {} '{}' world {}", hook.UserId.Value, hook.DisplayName, hook.WorldId);
        }

        private void CloseSession(SessionHook hook)
        {
            serverSessionGate.CloseGrant(hook.UserId, hook.WorldId);

            IReadOnlyList<int> kicked = serverSessionGate.ConnIdsOf(hook.UserId, hook.WorldId);
            foreach (int connId in kicked)
                serverTransport.Kick(connId);

            Log.Info("Session closed: user {} world {}, kicked online {}",
                hook.UserId == null ? "*" : hook.UserId.Value.ToString(), hook.WorldId, kicked.Count);
        }
    }
}
