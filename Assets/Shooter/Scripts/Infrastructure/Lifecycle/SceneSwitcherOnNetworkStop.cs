using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Infrastructure
{
    public class SceneSwitcherOnNetworkStop : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private string scene;

        private NetworkManager network;
        private bool switching;

        protected override void ShooterAwake()
        {
            network = NetworkManager.Singleton;
            network.OnClientStopped += Stopped;
            network.OnServerStopped += Stopped;
        }

        // The network may have stopped while this scene was still loading, and then there is no stop left to hear
        private void Start()
        {
            if (!network.IsListening) Stopped(false);
        }

        protected override void ShooterOnDestroy()
        {
            network.OnClientStopped -= Stopped;
            network.OnServerStopped -= Stopped;
        }

        private void Stopped(bool ignored)
        {
            if (switching) return;

            switching = true;
            Log.Info($"The network has stopped, switching to {scene}");
            SceneManager.LoadScene(scene);
        }
    }
}
