using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shooter.Configuring;
using Shooter.Logging;

namespace Shooter.Game.Core.Saves
{
    public static class MainCompressionManager
    {
        private static readonly Journal Log = Logs.Here();

        private static Dictionary<string, CompressionManager> byKey;
        private static Dictionary<string, CompressionManager> byExtension;
        private static List<CompressionManager> known;

        public static IEnumerable<string> Keys
        {
            get
            {
                Discover();
                return known.Select(manager => manager.Key);
            }
        }

        public static string Compress(string path)
        {
            Discover();

            string algorithm = Config.Read().Server.SaveCompressionAlgorithm.ToLowerInvariant();
            if (!byKey.TryGetValue(algorithm, out CompressionManager manager))
            {
                Log.Warn($"No compression manager for '{algorithm}', {path} stays as is");
                return path;
            }

            Log.Info($"{path} will be stored with {manager.Key}");
            return manager.Compress(path);
        }

        public static byte[] Read(string location, string file)
        {
            CompressionManager manager = Resolve(location);
            return manager == null ? null : manager.Read(location, file);
        }

        public static void Delete(string location)
        {
            Resolve(location)?.Delete(location);
        }

        private static CompressionManager Resolve(string location)
        {
            Discover();

            string extension = Directory.Exists(location) ? "" : Path.GetExtension(location).ToLowerInvariant();
            if (byExtension.TryGetValue(extension, out CompressionManager manager)) return manager;

            Log.Warn($"No compression manager for '{extension}' of {location}");
            return null;
        }

        private static void Discover()
        {
            if (known != null) return;

            byKey = new Dictionary<string, CompressionManager>();
            byExtension = new Dictionary<string, CompressionManager>();
            known = new List<CompressionManager>();

            IEnumerable<Type> kinds = typeof(CompressionManager).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && typeof(CompressionManager).IsAssignableFrom(type))
                .OrderBy(type => type.Name, StringComparer.Ordinal);

            foreach (Type kind in kinds)
            {
                var manager = (CompressionManager)Activator.CreateInstance(kind);
                string normalizedKey = manager.Key.ToLowerInvariant();
                string normalizedExtension = manager.Extension.ToLowerInvariant();
                if (byKey.ContainsKey(normalizedKey) || byExtension.ContainsKey(normalizedExtension))
                {
                    Log.Warn($"Compression manager {kind.Name} duplicates an already known one ({normalizedKey} - {normalizedExtension})");
                    continue;
                }

                byKey.Add(normalizedKey, manager);
                byExtension.Add(normalizedExtension, manager);
                known.Add(manager);
            }

            Log.Info($"{known.Count} compression managers are known");
        }
    }
}
