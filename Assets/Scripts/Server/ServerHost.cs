using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Server.Protocol;
using Shooter.Client.Input;
using Shooter.Server.Worlds.Entities.Players;
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
            Log.Info("WS listening on " + Port + ", tick rate " + TickRate);
        }

        private void OnDestroy()
        {
            serverTransport?.Stop();
        }

        private static bool TryLoadSecret(out byte[] secret)
        {
            secret = null;
            string raw = Environment.GetEnvironmentVariable("JWT_SECRET");
            if (string.IsNullOrEmpty(raw))
            {
                Log.Error("No JWT_SECRET env, refusing to start");
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
            Log.Info("Scene " + scene.name + " ready");
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
            if (!serverSessionGate.TryAdmit(connId, query, out ServerSession session))
            {
                serverTransport.Kick(connId);
                return;
            }
            serverTransport.Send(connId, Message.Encode(MessageType.Welcome, new Welcome { PlayerId = session.UserId, TickRate = (int)TickRate }));
        }

        private void OnMessageReceived(int connId, string json)
        {
            if (!serverSessionGate.TryGet(connId, out ServerSession session)) return;

            Message message = Message.Decode(json);
            if (message == null) return;

            switch (message.Type)
            {
                case MessageType.Hello:
                    Hello hello = message.Read<Hello>();
                    if (!string.IsNullOrEmpty(hello.Name))
                        session.DisplayName = hello.Name.Length > 40 ? hello.Name.Substring(0, 40) : hello.Name;
                    Log.Info("Conn " + connId + " hello: user " + session.UserId + " name '" + session.DisplayName + "'");
                    break;
                case MessageType.JoinWorld:
                    if (!session.InWorld)
                        EnterWorld(session);
                    break;
                case MessageType.PlayerIntent:
                    if (session.InWorld && worlds.TryGetValue(session.WorldId, out ServerWorld world))
                        world.ApplyInput(session.UserId, message.Read<PlayerIntent>());
                    break;
            }
        }

        private void EnterWorld(ServerSession session)
        {
            ServerWorld world = WorldFor(session.WorldId);
            world.AddPlayer(session.UserId, session.DisplayName);
            session.InWorld = true;

            serverTransport.Send(session.ConnId, Message.Encode(MessageType.WorldJoined, new WorldJoined
            {
                WorldId = world.Id,
                Players = world.BuildPlayerStates()
            }));

            string joined = Message.Encode(MessageType.PlayerJoined, new PlayerJoined { Id = session.UserId, Name = session.DisplayName });
            foreach (int connId in serverSessionGate.ConnIdsInWorld(world.Id))
                if (connId != session.ConnId)
                    serverTransport.Send(connId, joined);

            Log.Info("User " + session.UserId + " joined world " + world.Id + ", players there now " + world.Online());
        }

        private ServerWorld WorldFor(string worldId)
        {
            if (!worlds.TryGetValue(worldId, out ServerWorld world))
            {
                world = new ServerWorld(worldId);
                worlds[worldId] = world;
                Log.Info("World " + worldId + " created, total worlds " + worlds.Count);
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
                if (world.Online() == 0) continue;
                string json = Message.Encode(MessageType.Snapshot, world.BuildSnapshot(tick));
                foreach (int connId in serverSessionGate.ConnIdsInWorld(world.Id))
                    serverTransport.Send(connId, json);
            }
        }

        private void OnHookReceived(string json)
        {
            foreach (int connId in serverSessionGate.HandleHook(json))
                serverTransport.Kick(connId);
        }

        private void OnClientDisconnected(int connId)
        {
            if (!serverSessionGate.TryGet(connId, out ServerSession session)) return;
            serverSessionGate.Remove(connId);

            if (!session.InWorld) return;

            if (worlds.TryGetValue(session.WorldId, out ServerWorld world))
            {
                world.RemovePlayer(session.UserId);
                string left = Message.Encode(MessageType.PlayerLeft, new PlayerLeft { Id = session.UserId });
                foreach (int otherConnId in serverSessionGate.ConnIdsInWorld(session.WorldId))
                    serverTransport.Send(otherConnId, left);
            }

            Log.Info("User " + session.UserId + " disconnected from world " + session.WorldId + ", sessions total " + serverSessionGate.Count);
        }
    }
}
