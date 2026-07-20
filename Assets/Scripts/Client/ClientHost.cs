using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Shooter.Client.Account;
using Shooter.Client.Entities.Chronology;
using Shooter.Client.Entities.Players;
using Shooter.Client.Hud;
using Shooter.Client.Transport;
using Shooter.Client.Worlds;
using Shooter.Logging;
using Shooter.Server.Protocol;
using Shooter.Server.Sessions;
using Shooter.Server.Worlds;

namespace Shooter.Client
{
    public class ClientHost : MonoBehaviour
    {
        private const float InputSendRate = 30f;
        private const string GameScene = "Game";
        private const string MenuScene = "Menu";
        private const string MapScene = "Map";
        private const string RigPrefab = "PlayerRig";

        private IClientTransport clientTransport;
        private GameObject rigObject;
        private ClientWorld world;
        private PlayerRig rig;
        private HudRoot hud;
        private ClockView sky;

        private long playerId = -1;
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

            if (SceneManager.GetActiveScene().name == GameScene)
                EnterGameScene();
        }

        private void OnDestroy()
        {
            Teardown();
        }

        private void Update()
        {
            clientTransport?.Poll();

            if (world == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Log.Info("Client: Escape pressed, leaving world for menu");
                Teardown();
                SceneManager.LoadScene(MenuScene);
                return;
            }

            float deltaTime = Time.deltaTime;
            rig.Tick(deltaTime);
            hud.Tick();
            sky.Tick();
            world.Interpolate(deltaTime);

            if (Time.time < nextInputTime) return;

            nextInputTime = Time.time + 1f / InputSendRate;
            clientTransport.Send(Message.Encode(MessageType.PlayerIntent, rig.BuildIntent()));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == GameScene)
                EnterGameScene();
        }

        private void EnterGameScene()
        {
            LoadMap();

            if (string.IsNullOrEmpty(Session.Token))
            {
                Log.Warn("Client: no session token, game scene stays offline");
                return;
            }

            clientTransport = new ClientWsTransport();
            clientTransport.Connected += OnConnected;
            clientTransport.MessageReceived += OnMessageReceived;
            clientTransport.Connect(Session.WsUrl);
            Log.Info("Client: connecting to " + Session.WsUrl);
        }

        private static void LoadMap()
        {
            if (SceneManager.GetSceneByName(MapScene).isLoaded) return;

            SceneManager.LoadScene(MapScene, LoadSceneMode.Additive);
            Log.Info("Client: map loaded additively for render");
        }

        private void OnConnected()
        {
            clientTransport.Send(Message.Encode(MessageType.Hello, new Hello { Name = Session.DisplayName }));
            Log.Info("Client: hello sent as '" + Session.DisplayName + "'");
        }

        private void OnMessageReceived(string json)
        {
            Message message = Message.Decode(json);
            if (message == null) return;

            switch (message.Type)
            {
                case MessageType.Welcome:
                    Welcome welcome = message.Read<Welcome>();
                    playerId = welcome.PlayerId;
                    Log.Info("Client: welcome, player " + playerId + ", tick rate " + welcome.TickRate);
                    clientTransport.Send(Message.Encode(MessageType.JoinWorld, new JoinWorld()));
                    break;
                case MessageType.WorldJoined:
                    WorldJoined worldJoined = message.Read<WorldJoined>();
                    BuildWorld();
                    Log.Info("Client: joined world " + worldJoined.WorldId + ", players there " + worldJoined.Players.Count);
                    break;
                case MessageType.Snapshot:
                    world?.Apply(message.Read<Snapshot>());
                    break;
            }
        }

        private void BuildWorld()
        {
            world = new ClientWorld(playerId);
            rigObject = Instantiate(Resources.Load<GameObject>(RigPrefab));
            rig = new PlayerRig(rigObject.transform, world);
            hud = new HudRoot(rigObject.GetComponentInChildren<UIDocument>().rootVisualElement, world, rig.Aim);
            sky = new ClockView(world);

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            Log.Info("Client: rig, hud and sky built for player " + playerId);
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
            sky = null;
            playerId = -1;

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            Log.Info("Client: world torn down");
        }
    }
}
