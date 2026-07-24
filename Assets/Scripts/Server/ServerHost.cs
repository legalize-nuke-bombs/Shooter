using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Serialization;
using Shooter.Server.Control;
using Shooter.Server.Protocol;
using Shooter.Server.Sessions;
using Shooter.Server.Transport;
using Shooter.Server.Worlds;

namespace Shooter.Server
{
    public class ServerHost : MonoBehaviour
    {
        private const float TickRate = 30f;
        private const int Port = 9090;
        private const int ExcerptLength = 200;

        private IServerTransport serverTransport;
        private ServerSessionGate serverSessionGate;
        private ServerControlApi serverControlApi;
        private readonly ServerWorlds worlds = new ServerWorlds();
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
            Log.Info("ServerHost starting...");
            Application.runInBackground = true;
            Application.targetFrameRate = (int)TickRate * 2;

            if (!TryLoadSecret(out byte[] secret))
            {
                enabled = false;
                Application.Quit(1);
                return;
            }
            serverSessionGate = new ServerSessionGate(secret);

            serverTransport = new ServerWsTransport(new HookAuthority(secret));
            serverControlApi = new ServerControlApi(serverSessionGate, serverTransport);
            serverTransport.ClientConnected += OnClientConnected;
            serverTransport.MessageReceived += OnMessageReceived;
            serverTransport.ClientDisconnected += OnClientDisconnected;
            serverTransport.HookReceived += serverControlApi.Handle;
            serverTransport.Start(Port);
            Log.Info("WS listening on {}, tick rate {}", Port, TickRate);
        }

        private void OnDestroy()
        {
            serverTransport?.Stop();
            worlds.CloseAll();
        }

        public void EnterWorld(ServerSession session)
        {
            if (session.InWorld)
            {
                Log.Info("Conn {} user {} is already in world {}, join ignored", session.ConnId, session.UserId, session.WorldId);
                return;
            }

            ServerWorld world = worlds.Open(session.WorldId);
            Guid you = world.AddPlayer(session.UserId, session.DisplayName);
            session.InWorld = true;

            Send(session.ConnId, new WorldJoined
            {
                WorldId = world.Id,
                You = you
            });

            Log.Info("User {} joined world {} as entity {}, players there now {}", session.UserId, world.Id, you, world.Online);
        }

        public void ApplyInput(ServerSession session, PlayerIntent intent)
        {
            if (!session.InWorld) return;
            if (!worlds.TryGet(session.WorldId, out ServerWorld world)) return;

            world.ApplyInput(session.UserId, intent);
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
            Log.Info("Scene {} ready", scene.name);
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
        }

        private void OnClientConnected(int connId, string query)
        {
            if (!serverSessionGate.TryAdmit(connId, query, out ServerSession session))
            {
                serverTransport.Kick(connId);
                return;
            }

            Send(connId, new Welcome { UserId = session.UserId, TickRate = (int)TickRate });
        }

        private void OnMessageReceived(int connId, string json)
        {
            if (!serverSessionGate.TryGet(connId, out ServerSession session))
            {
                Log.Warn("Conn {} sent a message without a session, ignored", connId);
                return;
            }

            ServerBound message = Json.Deserialize<ServerBound>(json);
            if (message == null)
            {
                Log.Warn("Conn {} sent an unreadable message: {}", connId, Excerpt(json));
                return;
            }

            try
            {
                message.Apply(this, session);
            }
            catch (Exception e)
            {
                Log.Error("Conn {} message {} failed: {}", connId, message.GetType().Name, e);
            }
        }

        private void Simulate(float dt)
        {
            worlds.Tick(dt);
            serverSessionGate.Tick(dt);
        }

        private void BroadcastSnapshots()
        {
            foreach (ServerWorld world in worlds.Populated())
            {
                string json = Json.Serialize(world.BuildSnapshot(tick), typeof(ClientBound));
                foreach (int connId in serverSessionGate.ConnIdsInWorld(world.Id))
                    serverTransport.Send(connId, json);
            }
        }

        private void OnClientDisconnected(int connId)
        {
            if (!serverSessionGate.TryGet(connId, out ServerSession session)) return;
            serverSessionGate.Remove(connId);

            if (!session.InWorld) return;

            if (worlds.TryGet(session.WorldId, out ServerWorld world))
            {
                world.RemovePlayer(session.UserId);
                worlds.CloseWhenEmpty(session.WorldId);
            }

            Log.Info("User {} disconnected from world {}, sessions total {}", session.UserId, session.WorldId, serverSessionGate.Count);
        }

        private void Send(int connId, ClientBound message)
        {
            serverTransport.Send(connId, Json.Serialize(message, typeof(ClientBound)));
        }

        private static string Excerpt(string json)
        {
            if (string.IsNullOrEmpty(json)) return "";
            return json.Length <= ExcerptLength ? json : json.Substring(0, ExcerptLength) + "...";
        }
    }
}
