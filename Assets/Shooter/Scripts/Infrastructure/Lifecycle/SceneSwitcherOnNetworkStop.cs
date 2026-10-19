using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Infrastructure
{
    public class SceneSwitcherOnNetworkStop : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private string scene;

        private NetworkManager network;
        private bool switching;

        protected override void ShooterAwake()
        {
            network = NetworkManager.Singleton;
            network.OnClientStopped += HandleNetworkStopped;
            network.OnServerStopped += HandleNetworkStopped;
        }

        // The network may have stopped while this scene was still loading, and then there is no stop left to hear
        private void Start()
        {
            if (!network.IsListening) SwitchScene();
        }

        protected override void ShooterOnDestroy()
        {
            network.OnClientStopped -= HandleNetworkStopped;
            network.OnServerStopped -= HandleNetworkStopped;
        }

        private void HandleNetworkStopped(bool wasHost)
        {
            SwitchScene();
        }

        private void SwitchScene()
        {
            if (switching) return;

            switching = true;
            Log.Info($"The network has stopped, switching to {scene}");
            SceneManager.LoadScene(scene);
        }
    }
}
