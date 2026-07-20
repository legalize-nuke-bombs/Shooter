using Shooter.Logging;
using UnityEngine;

namespace Shooter.Client
{
    public static class ClientBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (Application.isBatchMode) return;

            Log.ToFile(System.IO.Path.Combine(Application.persistentDataPath, "shooter-client.log"));
            Log.Info("Bootstrapping client...");

            var go = new GameObject("ClientHost");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ClientHost>();
        }
    }
}
