using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Net;
using Shooter.Player;
using Shooter.Auth;

namespace Shooter.GameServer
{
    public class GameServer : MonoBehaviour
    {
        private const float TickRate = 30f;
        private const float WorldSpacing = 1000f;
        private const float AllowSweepInterval = 60f;
        private const long AllowTtlSeconds = 60;

        private INetTransport transport;
        private readonly Dictionary<int, ServerPlayer> players = new Dictionary<int, ServerPlayer>();
        private readonly Dictionary<string, int> worldIndices = new Dictionary<string, int>();
        private readonly AllowList allows = new AllowList();
        private byte[] jwtSecret;
        private float tickTimer;
        private float allowSweepTimer;
        private long tick;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.runInBackground = true;
            Application.targetFrameRate = (int)TickRate * 2;

            int port = ServerCli.IntArg("-port", 9090);
            string secret = ServerCli.StringArg("-jwtSecret", Environment.GetEnvironmentVariable("JWT_SECRET") ?? "");
            if (string.IsNullOrEmpty(secret))
            {
                ServerLog.Error("no JWT secret (-jwtSecret arg or JWT_SECRET env), refusing to start");
                Application.Quit(1);
                return;
            }
            jwtSecret = Convert.FromBase64String(secret);

            transport = new WsTransport();
            transport.ClientConnected += OnClientConnected;
            transport.MessageReceived += OnMessageReceived;
            transport.ClientDisconnected += OnClientDisconnected;
            transport.HookReceived += OnHookReceived;
            transport.HookAuthorizer = token => Jwt.TryVerify(token, jwtSecret, out string subject) && subject == "hook";
            transport.Start(port);
            ServerLog.Info("ws listening on " + port + ", tick rate " + TickRate + ", allow ttl " + AllowTtlSeconds + "s");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayerController localPlayer = FindAnyObjectByType<PlayerController>();
            if (localPlayer != null)
                Destroy(localPlayer.gameObject);
            ServerLog.Info("scene " + scene.name + " ready");
        }

        private void Update()
        {
            transport.Poll();

            tickTimer += Time.deltaTime;
            float tickInterval = 1f / TickRate;
            while (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;
                Simulate(tickInterval);
                tick++;
                BroadcastSnapshots();
            }

            allowSweepTimer += Time.deltaTime;
            if (allowSweepTimer >= AllowSweepInterval)
            {
                allowSweepTimer = 0f;
                int swept = allows.Sweep(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                if (swept > 0)
                    ServerLog.Info("swept " + swept + " expired allows");
            }
        }

        private void OnClientConnected(int connId, string query)
        {
            string token = ExtractQueryParam(query, "token");
            if (!Jwt.TryVerify(token, jwtSecret, out string subject))
            {
                ServerLog.Warn("conn " + connId + " token rejected, kicking");
                transport.Kick(connId);
                return;
            }

            if (!long.TryParse(subject, out long userId))
            {
                ServerLog.Warn("conn " + connId + " not a user token (sub '" + subject + "'), kicking");
                transport.Kick(connId);
                return;
            }

            if (!allows.TryConsume(userId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), out string worldId))
            {
                ServerLog.Warn("conn " + connId + " user " + userId + " has no open session, kicking");
                transport.Kick(connId);
                return;
            }

            var player = new ServerPlayer
            {
                ConnId = connId,
                UserId = userId,
                DisplayName = "player" + userId,
                WorldId = worldId
            };
            players[connId] = player;

            ServerLog.Info("conn " + connId + " authed: user " + player.UserId + " world " + player.WorldId);
            transport.Send(connId, NetJson.Serialize(new WelcomeMsg { type = "welcome", playerId = player.UserId, tickRate = (int)TickRate }));
        }

        private void OnMessageReceived(int connId, string json)
        {
            if (!players.TryGetValue(connId, out ServerPlayer player)) return;

            switch (NetJson.PeekType(json))
            {
                case "hello":
                    var hello = NetJson.Parse<HelloMsg>(json);
                    if (!string.IsNullOrEmpty(hello.name))
                        player.DisplayName = hello.name.Length > 40 ? hello.name.Substring(0, 40) : hello.name;
                    ServerLog.Info("conn " + connId + " hello: user " + player.UserId + " name '" + player.DisplayName + "'");
                    break;
                case "joinWorld":
                    if (!player.InWorld)
                        JoinWorld(player);
                    break;
                case "input":
                    var input = NetJson.Parse<InputMsg>(json);
                    player.LastInput = input;
                    if (input.jump) player.JumpQueued = true;
                    break;
            }
        }

        private void JoinWorld(ServerPlayer player)
        {
            player.InWorld = true;
            SpawnBody(player);

            var states = new List<PlayerStateMsg>();
            foreach (ServerPlayer p in players.Values)
                if (p.InWorld && p.WorldId == player.WorldId)
                    states.Add(BuildState(p));

            transport.Send(player.ConnId, NetJson.Serialize(new WorldJoinedMsg
            {
                type = "worldJoined",
                worldId = player.WorldId,
                players = states.ToArray()
            }));

            string joined = NetJson.Serialize(new JoinedMsg { type = "joined", id = player.UserId, name = player.DisplayName });
            foreach (ServerPlayer p in players.Values)
                if (p.InWorld && p.WorldId == player.WorldId && p.ConnId != player.ConnId)
                    transport.Send(p.ConnId, joined);

            ServerLog.Info("user " + player.UserId + " joined world " + player.WorldId + ", players there now " + states.Count);
        }

        private float WorldOffsetX(string worldId)
        {
            if (!worldIndices.TryGetValue(worldId, out int index))
            {
                index = worldIndices.Count;
                worldIndices[worldId] = index;
            }
            return index * WorldSpacing;
        }

        private void SpawnBody(ServerPlayer player)
        {
            var body = new GameObject("Sim_" + player.UserId);

            float angle = (player.ConnId * 137f) % 360f;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 16f;
            body.transform.position = new Vector3(WorldOffsetX(player.WorldId) + offset.x, 1.1f, offset.z);

            player.Body = body;
            player.Controller = body.AddComponent<CharacterController>();
            ServerLog.Info("spawned user " + player.UserId + " world " + player.WorldId + " at " + body.transform.position);
        }

        private void Simulate(float dt)
        {
            foreach (ServerPlayer p in players.Values)
            {
                if (!p.InWorld || p.Controller == null) continue;

                var input = new MotorInput
                {
                    MoveX = p.LastInput.moveX,
                    MoveZ = p.LastInput.moveZ,
                    Sprint = p.LastInput.sprint,
                    Jump = p.JumpQueued,
                    Yaw = p.LastInput.yaw
                };
                float verticalVelocity = p.VerticalVelocity;
                PlayerMotor.Step(p.Controller, ref verticalVelocity, input, dt);
                p.VerticalVelocity = verticalVelocity;
                p.JumpQueued = false;
            }
        }

        private void BroadcastSnapshots()
        {
            var statesByWorld = new Dictionary<string, List<PlayerStateMsg>>();
            foreach (ServerPlayer p in players.Values)
            {
                if (!p.InWorld) continue;
                if (!statesByWorld.TryGetValue(p.WorldId, out List<PlayerStateMsg> list))
                {
                    list = new List<PlayerStateMsg>();
                    statesByWorld[p.WorldId] = list;
                }
                list.Add(BuildState(p));
            }

            var jsonByWorld = new Dictionary<string, string>();
            foreach (KeyValuePair<string, List<PlayerStateMsg>> pair in statesByWorld)
                jsonByWorld[pair.Key] = NetJson.Serialize(new SnapshotMsg
                {
                    type = "snapshot",
                    tick = tick,
                    players = pair.Value.ToArray()
                });

            foreach (ServerPlayer p in players.Values)
                if (p.InWorld)
                    transport.Send(p.ConnId, jsonByWorld[p.WorldId]);
        }

        private PlayerStateMsg BuildState(ServerPlayer p)
        {
            Vector3 pos = p.Body.transform.position;
            return new PlayerStateMsg
            {
                id = p.UserId,
                name = p.DisplayName,
                x = pos.x,
                y = pos.y,
                z = pos.z,
                yaw = p.Body.transform.eulerAngles.y,
                pitch = p.LastInput.pitch
            };
        }

        private void OnHookReceived(string json)
        {
            UnityHookMsg hook = NetJson.Parse<UnityHookMsg>(json);
            if (hook == null || string.IsNullOrEmpty(hook.action) || string.IsNullOrEmpty(hook.worldId))
            {
                ServerLog.Warn("malformed hook, ignoring");
                return;
            }

            switch (hook.action)
            {
                case "OPEN_SESSION":
                    allows.Open(hook.userId, hook.worldId, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + AllowTtlSeconds);
                    ServerLog.Info("session opened: user " + hook.userId + " world " + hook.worldId);
                    break;
                case "CLOSE_SESSION":
                    CloseSessions(hook.userId, hook.worldId);
                    break;
                default:
                    ServerLog.Warn("unknown hook action " + hook.action + ", ignoring");
                    break;
            }
        }

        private void CloseSessions(long userId, string worldId)
        {
            bool wholeWorld = userId == 0;
            if (wholeWorld) allows.CloseWorld(worldId);
            else allows.Close(userId, worldId);

            var toKick = new List<int>();
            foreach (ServerPlayer p in players.Values)
                if (p.WorldId == worldId && (wholeWorld || p.UserId == userId))
                    toKick.Add(p.ConnId);
            foreach (int connId in toKick)
                transport.Kick(connId);

            ServerLog.Info("session closed: user " + (wholeWorld ? "*" : userId.ToString()) + " world " + worldId + ", kicked online " + toKick.Count);
        }

        private void OnClientDisconnected(int connId)
        {
            if (!players.TryGetValue(connId, out ServerPlayer player)) return;
            players.Remove(connId);

            if (player.Body != null) Destroy(player.Body);
            if (!player.InWorld) return;

            string left = NetJson.Serialize(new LeftMsg { type = "left", id = player.UserId });
            foreach (ServerPlayer p in players.Values)
                if (p.InWorld && p.WorldId == player.WorldId)
                    transport.Send(p.ConnId, left);

            ServerLog.Info("user " + player.UserId + " disconnected from world " + player.WorldId + ", players total " + players.Count);
        }

        private void OnDestroy()
        {
            transport?.Stop();
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
