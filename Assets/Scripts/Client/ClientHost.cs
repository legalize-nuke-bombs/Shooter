using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Shooter.Client.Account;
using Shooter.Client.Hud;
using Shooter.Client.Transport;
using Shooter.Client.Worlds;
using Shooter.Client.Worlds.Entities.Chronology;
using Shooter.Client.Worlds.Entities.Players;
using Shooter.Logging;
using Shooter.Serialization;
using Shooter.Server.Protocol;

namespace Shooter.Client
{
    public class ClientHost : MonoBehaviour
    {
        private const float InputSendRate = 30f;
        private const string GameScene = "Game";
        private const string MenuScene = "Menu";
        private const string MapScene = "Map";
        private const string RigPrefab = "PlayerRig";
        private const string MenuPrefab = "MenuRoot";
        private const int ExcerptLength = 200;

        private IClientTransport clientTransport;
        private GameObject rigObject;
        private ClientWorld world;
        private PlayerRig rig;
        private HudRoot hud;
        private ClockView clockView;

        private long myUserId;
        private float nextInputTime;

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
            Log.Info("ClientHost starting...");

            EnterScene(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            Teardown();
        }

        private void OnWelcome(Welcome welcome)
        {
            myUserId = welcome.UserId;
            Log.Info("Welcome, user {}, tick rate {}", welcome.UserId, welcome.TickRate);
            Send(new JoinWorld());
        }

        private void OnWorldJoined(WorldJoined worldJoined)
        {
            BuildWorld(worldJoined.You);
            Log.Info("Joined world {} as entity {}", worldJoined.WorldId, worldJoined.You);
        }

        private void OnSnapshot(Snapshot snapshot)
        {
            world?.Apply(snapshot);
        }

        private void Update()
        {
            clientTransport?.Poll();

            if (world == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame && !hud.Escape())
            {
                Log.Info("Escape pressed, leaving world for menu");
                Teardown();
                SceneManager.LoadScene(MenuScene);
                return;
            }

            float deltaTime = Time.deltaTime;
            rig.Tick(deltaTime);
            hud.Tick(deltaTime);
            clockView.Render();
            world.Tick(deltaTime);

            if (Time.time < nextInputTime) return;

            nextInputTime = Time.time + 1f / InputSendRate;
            Send(rig.BuildIntent());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnterScene(scene.name);
        }

        private void EnterScene(string sceneName)
        {
            if (sceneName == MenuScene)
                EnterMenuScene();
            else if (sceneName == GameScene)
                EnterGameScene();
        }

        private void EnterMenuScene()
        {
            Instantiate(Resources.Load<GameObject>(MenuPrefab));
            Log.Info("Menu built");
        }

        private void EnterGameScene()
        {
            LoadMap();

            if (string.IsNullOrEmpty(Session.Token))
            {
                Log.Warn("No session token, game scene stays offline");
                return;
            }

            clientTransport = new ClientWsTransport();
            clientTransport.MessageReceived += OnMessageReceived;
            clientTransport.Connect(Session.WsUrl);
            Log.Info("Connecting to {}", Session.WsUrl);
        }

        private static void LoadMap()
        {
            if (SceneManager.GetSceneByName(MapScene).isLoaded) return;

            SceneManager.LoadScene(MapScene, LoadSceneMode.Additive);
            Log.Info("Map loaded additively for render");
        }

        private void OnMessageReceived(string json)
        {
            ClientBound message = Json.Deserialize<ClientBound>(json);

            try
            {
                switch (message)
                {
                    case Welcome welcome:
                        OnWelcome(welcome);
                        break;
                    case WorldJoined worldJoined:
                        OnWorldJoined(worldJoined);
                        break;
                    case Snapshot snapshot:
                        OnSnapshot(snapshot);
                        break;
                    default:
                        Log.Warn("Server sent an unreadable message: {}", Excerpt(json));
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error("Message {} from server failed: {}", message?.GetType().Name, e);
            }
        }

        private void BuildWorld(Guid myId)
        {
            rigObject = Instantiate(Resources.Load<GameObject>(RigPrefab));
            rig = new PlayerRig(rigObject.transform);
            world = new ClientWorld(myId, myUserId, rigObject.transform);

            hud = new HudRoot(world, rig);
            rigObject.GetComponentInChildren<UIDocument>().rootVisualElement.Add(hud);

            clockView = new ClockView(world);

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            Log.Info("Rig, hud and sky built for entity {}", myId);
        }

        private void Send(ServerBound message)
        {
            clientTransport?.Send(Json.Serialize(message, typeof(ServerBound)));
        }

        private void Teardown()
        {
            if (world == null && clientTransport == null) return;

            clientTransport?.Stop();
            clientTransport = null;

            world?.Destroy();
            world = null;

            if (rigObject != null) Destroy(rigObject);
            rigObject = null;
            rig = null;
            hud = null;
            clockView = null;

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            Log.Info("World torn down");
        }

        private static string Excerpt(string json)
        {
            if (string.IsNullOrEmpty(json)) return "";

            return json.Length <= ExcerptLength ? json : json.Substring(0, ExcerptLength) + "...";
        }
    }
}
