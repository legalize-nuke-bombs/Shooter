using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;

namespace Shooter.Editor
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

            if (network.IsListening)
            {
                network.Shutdown();
                return;
            }

            // A start that failed on bind can leave the driver holding the port. After an ordinary session the driver is
            // already gone, and shutting the transport down by hand once more would only make Netcode warn
            NetworkTransport transport = network.NetworkConfig == null ? null : network.NetworkConfig.NetworkTransport;
            if (transport == null || transport is UnityTransport unity && !unity.GetNetworkDriver().IsCreated) return;

            transport.Shutdown();
        }
    }
}
