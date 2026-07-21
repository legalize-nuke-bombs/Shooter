using System;
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

            Log.ToFile(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "shooter-client.log"));
            Log.Info("Bootstrapping client...");

            var go = new GameObject("ClientHost");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ClientHost>();
        }
    }
}
