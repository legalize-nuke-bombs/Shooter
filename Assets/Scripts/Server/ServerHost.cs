using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Protocol;
using Shooter.Client.Input;
using Shooter.Server.Entities.Players;
using Shooter.Server.Sessions;
using Shooter.Server.Transport;
using Shooter.Server.Worlds;
using Shooter.Logging;

namespace Shooter.Server
{
    public class ServerHost : MonoBehaviour
    {
        private const float TickRate = 30f;
        private const int Port = 9090;

        private IServerTransport serverTransport;
        private ServerSessionGate serverSessionGate;
        private readonly Dictionary<string, ServerWorld> worlds = new Dictionary<string, ServerWorld>();
        private float tickTimer;
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

            if (!TryLoadSecret(out byte[] secret))
            {
                enabled = false;
                Application.Quit(1);
                return;
            }
            serverSessionGate = new ServerSessionGate(secret);

            serverTransport = new ServerWsTransport();
            serverTransport.ClientConnected += OnClientConnected;
            serverTransport.MessageReceived += OnMessageReceived;
            serverTransport.ClientDisconnected += OnClientDisconnected;
            serverTransport.HookReceived += OnHookReceived;
            serverTransport.HookAuthorizer = serverSessionGate.AuthorizeHook;
            serverTransport.Start(Port);
            Log.Info("ws listening on " + Port + ", tick rate " + TickRate);
        }

        private static bool TryLoadSecret(out byte[] secret)
        {
            secret = null;
            string raw = Environment.GetEnvironmentVariable("JWT_SECRET");
            if (string.IsNullOrEmpty(raw))
            {
                Log.Error("no JWT_SECRET env, refusing to start");
                return false;
            }
            try
            {
                secret = Convert.FromBase64String(raw);
            }
            catch (FormatException)
            {
                Log.Error("JWT_SECRET is not valid base64, refusing to start");
                return false;
            }
            return true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InputController localPlayer = FindAnyObjectByType<InputController>();
            if (localPlayer != null)
                Destroy(localPlayer.gameObject);
            Log.Info("scene " + scene.name + " ready");
        }

        private void Update()
        {
            serverTransport.Poll();

            tickTimer += Time.deltaTime;
            float tickInterval = 1f / TickRate;
            while (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;
                Simulate(tickInterval);
                tick++;
                BroadcastSnapshots();
            }

            serverSessionGate.Tick(Time.deltaTime);
        }

        private void OnClientConnected(int connId, string query)
        {
            if (!serverSessionGate.TryAdmit(connId, query, out Player player))
            {
                serverTransport.Kick(connId);
                return;
            }
            serverTransport.Send(connId, Message.Encode(MessageType.Welcome, new Welcome { PlayerId = player.UserId, TickRate = (int)TickRate }));
        }

        private void OnMessageReceived(int connId, string json)
        {
            if (!serverSessionGate.TryGet(connId, out Player player)) return;

            Message message = Message.Decode(json);
            if (message == null) return;

            switch (message.Type)
            {
                case MessageType.Hello:
                    Hello hello = message.Read<Hello>();
                    if (!string.IsNullOrEmpty(hello.Name))
                        player.DisplayName = hello.Name.Length > 40 ? hello.Name.Substring(0, 40) : hello.Name;
                    Log.Info("conn " + connId + " hello: user " + player.UserId + " name '" + player.DisplayName + "'");
                    break;
                case MessageType.JoinWorld:
                    if (!player.InWorld)
                        EnterWorld(player);
                    break;
                case MessageType.PlayerIntent:
                    player.ApplyInput(message.Read<PlayerIntent>());
                    break;
            }
        }

        private void EnterWorld(Player player)
        {
            ServerWorld world = WorldFor(player.WorldId);
            world.AddPlayer(player);
            player.InWorld = true;

            serverTransport.Send(player.ConnId, Message.Encode(MessageType.WorldJoined, new WorldJoined
            {
                WorldId = world.Id,
                Players = world.BuildPlayerStates()
            }));

            string joined = Message.Encode(MessageType.PlayerJoined, new PlayerJoined { Id = player.UserId, Name = player.DisplayName });
            foreach (Player other in world.Players)
                if (other.ConnId != player.ConnId)
                    serverTransport.Send(other.ConnId, joined);

            Log.Info("user " + player.UserId + " joined world " + world.Id + ", players there now " + world.Players.Count);
        }

        private ServerWorld WorldFor(string worldId)
        {
            if (!worlds.TryGetValue(worldId, out ServerWorld world))
            {
                world = new ServerWorld(worldId);
                worlds[worldId] = world;
                Log.Info("world " + worldId + " created, total worlds " + worlds.Count);
            }
            return world;
        }

        private void Simulate(float dt)
        {
            foreach (ServerWorld world in worlds.Values)
                world.Tick(dt);
        }

        private void BroadcastSnapshots()
        {
            foreach (ServerWorld world in worlds.Values)
            {
                if (world.Players.Count == 0) continue;
                string json = Message.Encode(MessageType.Snapshot, world.BuildSnapshot(tick));
                foreach (Player player in world.Players)
                    serverTransport.Send(player.ConnId, json);
            }
        }

        private void OnHookReceived(string json)
        {
            foreach (int connId in serverSessionGate.HandleHook(json))
                serverTransport.Kick(connId);
        }

        private void OnClientDisconnected(int connId)
        {
            if (!serverSessionGate.TryGet(connId, out Player player)) return;
            serverSessionGate.Remove(connId);

            if (player.Body != null) Destroy(player.Body);
            if (!player.InWorld) return;

            if (worlds.TryGetValue(player.WorldId, out ServerWorld world))
            {
                world.RemovePlayer(connId);
                string left = Message.Encode(MessageType.PlayerLeft, new PlayerLeft { Id = player.UserId });
                foreach (Player other in world.Players)
                    serverTransport.Send(other.ConnId, left);
            }

            Log.Info("user " + player.UserId + " disconnected from world " + player.WorldId + ", sessions total " + serverSessionGate.Count);
        }

        private void OnDestroy()
        {
            serverTransport?.Stop();
        }
    }
}
