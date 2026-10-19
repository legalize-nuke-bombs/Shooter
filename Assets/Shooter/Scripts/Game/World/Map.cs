using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Game
{
    // The map's own start and end. On the server it brings in the save the menu chose and hands out the bodies;
    // on every side it takes the loading screen down once in, and goes back to the menu when the network stops
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class Map : ShooterBehaviour
    {
        private const int UnlimitedFrameRate = -1;
        private const string BrokenSave = "Не удалось загрузить сохранение";
        private const string BrokenSaveDetails = "Файл повреждён или несовместим с этой версией";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private PlayerSpawner spawner;
        [SerializeField] private GameObject state;
        [SerializeField] private string menu = "Menu";

        private readonly Dictionary<Type, Component> parts = new();

        private NetworkManager network;
        private NetworkStarter starter;
        private LoadingScreenOverlay loading;
        private FrozenWorld frozen;
        private bool leaving;

        public static T Get<T>() where T : Component
        {
            foreach (Map map in Registers.Of<Map>(RegisterInactivePolicy.Include))
            {
                return map.Part<T>();
            }
            return null;
        }

        protected override void ShooterAwake()
        {
            if (Registers.Of<Map>(RegisterInactivePolicy.Include).Count() > 1) Log.Error("A second map has appeared");

            Application.targetFrameRate = UnlimitedFrameRate;
            network = NetworkManager.Singleton;
            starter = network.GetComponent<NetworkStarter>();
            loading = FindAnyObjectByType<LoadingScreenOverlay>();
            network.OnClientStopped += Stopped;
            network.OnServerStopped += Stopped;
            network.OnClientConnectedCallback += Entered;
            if (!network.IsServer) return;

            SaveManager.Open();
            network.SceneManager.OnLoadComplete += Loaded;
        }

        // The network may have stopped while this map was still loading, and then there is no stop left to hear
        private void Start()
        {
            if (!network.IsListening) Stopped(false);
        }

        protected override void ShooterOnDestroy()
        {
            network.OnClientStopped -= Stopped;
            network.OnServerStopped -= Stopped;
            network.OnClientConnectedCallback -= Entered;
            if (network.SceneManager != null) network.SceneManager.OnLoadComplete -= Loaded;
        }

        private void Loaded(ulong client, string scene, LoadSceneMode mode)
        {
            if (client != NetworkManager.ServerClientId || scene != gameObject.scene.name) return;

            network.SceneManager.OnLoadComplete -= Loaded;

            // Netcode calls this before the scene's first Update: freezing here keeps that one Update from running on
            // a world the save has not loaded yet
            if (starter.Save != null) frozen = SaveManager.Freeze();
            StartCoroutine(Begin());
        }

        private IEnumerator Begin()
        {
            yield return null;
            Log.Info($"Map {gameObject.scene.name} is up");

            if (frozen != null && !Restore()) yield break;

            spawner.Ready();
            loading.Hide();
        }

        private bool Restore()
        {
            Log.Info($"Loading the world from {starter.Save}");
            loading.Show(LoadingScreenStage.Save);
            if (SaveManager.Load(frozen, starter.Save)) return true;

            Log.Warn($"The world failed to load {starter.Save}, shutting the host down");
            starter.Ending = new Refusal(BrokenSave, BrokenSaveDetails);
            network.Shutdown();
            return false;
        }

        // A client connects only once its world is synchronized, so this is the moment it is in
        private void Entered(ulong client)
        {
            if (network.IsServer || client != network.LocalClientId) return;

            loading.Hide();
        }

        private void Stopped(bool ignored)
        {
            if (leaving) return;

            leaving = true;
            Log.Info("The session is over, heading back to the menu");
            SceneManager.LoadScene(menu);
        }

        private T Part<T>() where T : Component
        {
            if (parts.TryGetValue(typeof(T), out Component known)) return (T)known;

            T part = state.GetComponent<T>();
            if (part == null) Log.Error($"The map state carries no {typeof(T).Name}");

            parts[typeof(T)] = part;
            return part;
        }
    }
}
