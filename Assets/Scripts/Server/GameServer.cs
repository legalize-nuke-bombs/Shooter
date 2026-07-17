using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Net;
using Shooter.Entities.Player;
using Shooter.Entities.Chronology;
using Shooter.Logging;

namespace Shooter.Server
{
    public class GameServer : MonoBehaviour
    {
        private const float TickRate = 30f;
        private const int Port = 9090;
        private const float WorldSpacing = 1000f;
        private const float AllowSweepInterval = 60f;

        private INetTransport transport;
        private SessionGate sessions;
        private readonly Dictionary<string, World> worlds = new Dictionary<string, World>();
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
            Application.runInBackground = true;
            Application.targetFrameRate = (int)TickRate * 2;

            string secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "";
            if (string.IsNullOrEmpty(secret))
            {
                Log.Error("no JWT_SECRET env, refusing to start");
                Application.Quit(1);
                return;
            }
            sessions = new SessionGate(Convert.FromBase64String(secret));

            transport = new WsTransport();
            transport.ClientConnected += OnClientConnected;
            transport.MessageReceived += OnMessageReceived;
            transport.ClientDisconnected += OnClientDisconnected;
            transport.HookReceived += OnHookReceived;
            transport.HookAuthorizer = sessions.AuthorizeHook;
            transport.Start(Port);
            Log.Info("ws listening on " + Port + ", tick rate " + TickRate);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayerController localPlayer = FindAnyObjectByType<PlayerController>();
            if (localPlayer != null)
                Destroy(localPlayer.gameObject);
            Log.Info("scene " + scene.name + " ready");
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
                int swept = sessions.Sweep();
                if (swept > 0)
                    Log.Info("swept " + swept + " expired allows");
            }
        }

        private void OnClientConnected(int connId, string query)
        {
            if (!sessions.TryAdmit(connId, query, out ServerPlayer player))
            {
                transport.Kick(connId);
                return;
            }
            transport.Send(connId, NetJson.Serialize(new WelcomeMsg { type = "welcome", playerId = player.UserId, tickRate = (int)TickRate }));
        }

        private void OnMessageReceived(int connId, string json)
        {
            if (!sessions.TryGet(connId, out ServerPlayer player)) return;

            switch (NetJson.PeekType(json))
            {
                case "hello":
                    var hello = NetJson.Parse<HelloMsg>(json);
                    if (!string.IsNullOrEmpty(hello.name))
                        player.DisplayName = hello.name.Length > 40 ? hello.name.Substring(0, 40) : hello.name;
                    Log.Info("conn " + connId + " hello: user " + player.UserId + " name '" + player.DisplayName + "'");
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
            World world = WorldFor(player.WorldId);
            world.Add(player);
            player.InWorld = true;
            ServerPlayerSim.SpawnBody(player, world.OffsetX);

            transport.Send(player.ConnId, NetJson.Serialize(new WorldJoinedMsg
            {
                type = "worldJoined",
                worldId = world.Id,
                players = world.BuildStates()
            }));

            string joined = NetJson.Serialize(new JoinedMsg { type = "joined", id = player.UserId, name = player.DisplayName });
            foreach (ServerPlayer p in world.Players)
                if (p.ConnId != player.ConnId)
                    transport.Send(p.ConnId, joined);

            Log.Info("user " + player.UserId + " joined world " + world.Id + ", players there now " + world.Players.Count);
        }

        private World WorldFor(string worldId)
        {
            if (!worlds.TryGetValue(worldId, out World world))
            {
                world = new World(worldId, worlds.Count * WorldSpacing);
                worlds[worldId] = world;
            }
            return world;
        }

        private void Simulate(float dt)
        {
            foreach (World world in worlds.Values)
                world.Step(dt);
        }

        private void BroadcastSnapshots()
        {
            foreach (World world in worlds.Values)
            {
                if (world.Players.Count == 0) continue;
                string json = NetJson.Serialize(world.BuildSnapshot(tick));
                foreach (ServerPlayer p in world.Players)
                    transport.Send(p.ConnId, json);
            }
        }

        private void OnHookReceived(string json)
        {
            foreach (int connId in sessions.HandleHook(json))
                transport.Kick(connId);
        }

        private void OnClientDisconnected(int connId)
        {
            if (!sessions.TryGet(connId, out ServerPlayer player)) return;
            sessions.Remove(connId);

            if (player.Body != null) Destroy(player.Body);
            if (!player.InWorld) return;

            if (worlds.TryGetValue(player.WorldId, out World world))
            {
                world.Remove(connId);
                string left = NetJson.Serialize(new LeftMsg { type = "left", id = player.UserId });
                foreach (ServerPlayer p in world.Players)
                    transport.Send(p.ConnId, left);
            }

            Log.Info("user " + player.UserId + " disconnected from world " + player.WorldId + ", sessions total " + sessions.Count);
        }

        private void OnDestroy()
        {
            transport?.Stop();
        }
    }
}
