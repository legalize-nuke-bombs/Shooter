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
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

            NetworkManager.Singleton.Shutdown();
        }
    }
}
