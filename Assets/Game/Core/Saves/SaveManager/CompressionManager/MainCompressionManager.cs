using System;
using System.Collections.Generic;
using System.IO;
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
                string normalizedKey = manager.Key.ToLowerInvariant();
                string normalizedExtension = manager.Extension.ToLowerInvariant();
                if (byKey.ContainsKey(normalizedKey) || byExtension.ContainsKey(normalizedExtension))
                {
                    Log.Warn($"Entity {name} found manager duplicate {manager.name} ({normalizedKey} - {normalizedExtension})");
                    continue;
                }
                byKey.Add(normalizedKey, manager);
                byExtension.Add(normalizedExtension, manager);
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
            string algorithm = Config.Read().Server.SaveCompressionAlgorithm.ToLowerInvariant();
            CompressionManager manager;
            bool found = String.IsNullOrEmpty(algorithm)
                ? byExtension.TryGetValue("", out manager)
                : byKey.TryGetValue(algorithm, out manager);

            if (!found)
            {
                Log.Warn($"Entity {name} found no compression manager for '{algorithm}', {path} stays as is");
                return path;
            }

            Log.Info($"Entity {name} will store {path} with {manager.Key}");
            return manager.Compress(path);
        }

        public CompressionManager Resolve(string location)
        {
            string extension = Directory.Exists(location) ? "" : Path.GetExtension(location).ToLowerInvariant();
            return byExtension.TryGetValue(extension, out CompressionManager manager) ? manager : null;
        }
    }
}
