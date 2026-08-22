using System;
using System.Collections.Generic;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public class MainCompressionManager : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Dictionary<string, CompressionManager> byKey = new Dictionary<string, CompressionManager>();
        private Dictionary<string, CompressionManager> byExtension = new Dictionary<string, CompressionManager>();

        public static MainCompressionManager Current { get; private set; }

        private void Awake()
        {
            CompressionManager[] managers = GetComponents<CompressionManager>();
            foreach (CompressionManager manager in managers)
            {
                string normalizedKey = manager.Key.ToLower();
                if (byKey.ContainsKey(normalizedKey) || byExtension.ContainsKey(manager.Extension))
                {
                    Log.Warn($"Entity {name} found manager duplicate {manager.name} ({normalizedKey} - {manager.Extension})");
                    continue;
                }
                byKey.Add(normalizedKey, manager);
                byExtension.Add(manager.Extension, manager);
            }
            Log.Info($"Entity {name} knows {byKey.Count} - {byExtension.Count} compression managers");

            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public string Compress(string path)
        {
            string algorithm = Config.Read().Server.SaveCompressionAlgorithm.ToLower();
            if (String.IsNullOrEmpty(algorithm))
            {
                Log.Info($"Entity {name} will not search CompressionManager because algorithm is not set");
                return path;
            }

            if (!byKey.TryGetValue(algorithm, out CompressionManager manager))
            {
                Log.Warn($"Failed to find compression manager {algorithm}");
                return path;
            }

            Log.Info($"Entity {name} will compress {path} with {manager.Key}");
            return manager.Compress(path);
        }
    }
}
