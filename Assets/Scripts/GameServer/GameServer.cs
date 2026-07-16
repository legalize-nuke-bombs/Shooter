using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Net;
using Shooter.Player;
using Shooter.Shared;

namespace Shooter.GameServer
{
    public class GameServer : MonoBehaviour
    {
        private const float TickRate = 30f;
        private const float WorldSpacing = 1000f;
        private const float BanSweepInterval = 60f;
        private const long BanRetentionSeconds = 180;

        private INetTransport transport;
        private readonly Dictionary<int, ServerPlayer> players = new Dictionary<int, ServerPlayer>();
        private readonly Dictionary<string, int> worldIndices = new Dictionary<string, int>();
        private readonly BanList bans = new BanList();
        private byte[] jwtSecret;
        private float tickTimer;
        private float banSweepTimer;
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
            string secret = ServerCli.StringArg("-jwtSecret", Environment.GetEnvironmentVariable("UNITY_SERVER_SECRET") ?? "");
            if (string.IsNullOrEmpty(secret))
            {
                ServerLog.Error("no JWT secret (-jwtSecret arg or UNITY_SERVER_SECRET env), refusing to start");
                Application.Quit(1);
                return;
            }
            jwtSecret = Convert.FromBase64String(secret);

            transport = new WsTransport();
            transport.ClientConnected += OnClientConnected;
            transport.MessageReceived += OnMessageReceived;
            transport.ClientDisconnected += OnClientDisconnected;
            transport.HookReceived += OnHookReceived;
            transport.HookAuthorizer = token => JwtVerifier.TryVerify(token, jwtSecret, out JwtClaims claims) && claims.sub == "hook";
            transport.Start(port);
            ServerLog.Info("ws listening on " + port + ", tick rate " + TickRate + ", ban retention " + BanRetentionSeconds + "s");
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

            banSweepTimer += Time.deltaTime;
            if (banSweepTimer >= BanSweepInterval)
            {
                banSweepTimer = 0f;
                int swept = bans.Sweep(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - BanRetentionSeconds);
                if (swept > 0)
                    ServerLog.Info("swept " + swept + " expired bans");
            }
        }

        private void OnClientConnected(int connId, string query)
        {
            string token = ExtractQueryParam(query, "token");
            if (!JwtVerifier.TryVerify(token, jwtSecret, out JwtClaims claims))
            {
                ServerLog.Warn("conn " + connId + " token rejected, kicking");
                transport.Kick(connId);
                return;
            }

            string[] subject = claims.sub.Split(new[] { ':' }, 2);
            if (subject.Length != 2 || subject[1].Length == 0 || !long.TryParse(subject[0], out long userId))
            {
                ServerLog.Warn("conn " + connId + " not a world token (sub '" + claims.sub + "'), kicking");
                transport.Kick(connId);
                return;
            }

            string worldId = subject[1];

            if (bans.IsBanned(userId, worldId, claims.iat))
            {
                ServerLog.Warn("conn " + connId + " user " + userId + " world " + worldId + " pre-kick token (iat " + claims.iat + "), kicking");
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

            ServerLog.Info("conn " + connId + " authed: user " + player.UserId + " world " + player.WorldId + ", token iat " + claims.iat);
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
            HookBatchMsg batch = NetJson.Parse<HookBatchMsg>(json);
            if (batch == null || batch.hooks == null) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (UnityHookMsg hook in batch.hooks)
                ApplyHook(hook, now);
        }

        private void ApplyHook(UnityHookMsg hook, long now)
        {
            bool hasUser = hook.userIdToKick != 0;
            bool hasWorld = !string.IsNullOrEmpty(hook.worldIdToKick);

            if (hasUser && hasWorld) bans.BanPair(hook.userIdToKick, hook.worldIdToKick, now);
            else if (hasUser) bans.BanUser(hook.userIdToKick, now);
            else if (hasWorld) bans.BanWorld(hook.worldIdToKick, now);
            else
            {
                ServerLog.Warn("hook with no user and no world, ignoring");
                return;
            }

            var toKick = new List<int>();
            foreach (ServerPlayer p in players.Values)
            {
                bool userMatch = !hasUser || p.UserId == hook.userIdToKick;
                bool worldMatch = !hasWorld || p.WorldId == hook.worldIdToKick;
                if (userMatch && worldMatch)
                    toKick.Add(p.ConnId);
            }
            foreach (int connId in toKick)
                transport.Kick(connId);

            ServerLog.Info("hook applied: user " + hook.userIdToKick + " world " + hook.worldIdToKick + ", banned since now, kicked online " + toKick.Count);
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
