using Unity.Netcode;
using UnityEditor;

namespace Shooter.Editing
{
    [InitializeOnLoad]
    public static class PlayModeShutdown
    {
        static PlayModeShutdown()
        {
            EditorApplication.playModeStateChanged += Stop;
        }

        private static void Stop(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;

            NetworkManager network = NetworkManager.Singleton;
            if (network == null) return;

            try
            {
                if (network.IsListening) network.Shutdown();
            }
            finally
            {
                NetworkTransport transport = network.NetworkConfig == null ? null : network.NetworkConfig.NetworkTransport;
                if (transport != null) transport.Shutdown();
            }
        }
    }
}
