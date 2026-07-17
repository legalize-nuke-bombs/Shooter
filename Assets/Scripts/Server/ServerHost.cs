using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Serialization;
using Shooter.Client.Input;
using Shooter.Server.Entities.Characters.Player;
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
            serverTransport.Send(connId, Json.Serialize(new Welcome { playerId = player.UserId, tickRate = (int)TickRate }));
        }

        private void OnMessageReceived(int connId, string json)
        {
            if (!serverSessionGate.TryGet(connId, out Player player)) return;

            switch (Json.TypeOf(json))
            {
                case nameof(Hello):
                    Hello hello = Json.Deserialize<Hello>(json);
                    if (!string.IsNullOrEmpty(hello.name))
                        player.DisplayName = hello.name.Length > 40 ? hello.name.Substring(0, 40) : hello.name;
                    Log.Info("conn " + connId + " hello: user " + player.UserId + " name '" + player.DisplayName + "'");
                    break;
                case nameof(JoinWorld):
                    if (!player.InWorld)
                        EnterWorld(player);
                    break;
                case nameof(PlayerIntent):
                    player.ApplyInput(Json.Deserialize<PlayerIntent>(json));
                    break;
            }
        }

        private void EnterWorld(Player player)
        {
            ServerWorld world = WorldFor(player.WorldId);
            world.Add(player);
            player.InWorld = true;

            serverTransport.Send(player.ConnId, Json.Serialize(new WorldJoined
            {
                worldId = world.Id,
                players = world.BuildStates()
            }));

            string joined = Json.Serialize(new PlayerJoined { id = player.UserId, name = player.DisplayName });
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
                world.Step(dt);
        }

        private void BroadcastSnapshots()
        {
            foreach (ServerWorld world in worlds.Values)
            {
                if (world.Players.Count == 0) continue;
                string json = Json.Serialize(world.BuildSnapshot(tick));
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
                world.Remove(connId);
                string left = Json.Serialize(new PlayerLeft { id = player.UserId });
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
