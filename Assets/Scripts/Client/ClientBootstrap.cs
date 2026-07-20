using UnityEngine;

namespace Shooter.Client
{
    public static class ClientBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (Application.isBatchMode) return;

            var go = new GameObject("ClientHost");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ClientHost>();
        }
    }
}
