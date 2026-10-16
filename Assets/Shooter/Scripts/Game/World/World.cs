using System.Collections;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Game
{
    // The world's own start and end. On the server it brings in the save the menu chose and hands out the bodies;
    // on every side it takes the loading screen down once in, and goes back to the menu when the network stops
    public class World : MonoBehaviour
    {
        private const int UnlimitedFrameRate = -1;
        private const string BrokenSave = "Не удалось загрузить сохранение";
        private const string BrokenSaveDetails = "Файл повреждён или несовместим с этой версией";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Launch launch;
        [SerializeField] private Bodies bodies;
        [SerializeField] private string menu = "Menu";

        private NetworkManager network;
        private LoadingOverlay loading;
        private FrozenWorld frozen;
        private bool leaving;

        private void Awake()
        {
            Application.targetFrameRate = UnlimitedFrameRate;
            network = NetworkManager.Singleton;
            loading = FindAnyObjectByType<LoadingOverlay>();
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

        private void OnDestroy()
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
            if (launch.Save != null) frozen = SaveManager.Freeze();
            StartCoroutine(Begin());
        }

        private IEnumerator Begin()
        {
            yield return null;
            Log.Info($"World {gameObject.scene.name} is up");

            if (frozen != null && !Restore()) yield break;

            bodies.Ready();
            loading.Hide();
        }

        private bool Restore()
        {
            Log.Info($"Loading the world from {launch.Save}");
            loading.Show(LoadingStage.Save);
            if (SaveManager.Load(frozen, launch.Save)) return true;

            Log.Warn($"The world failed to load {launch.Save}, shutting the host down");
            launch.Ending = new Refusal(BrokenSave, BrokenSaveDetails);
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
    }
}
