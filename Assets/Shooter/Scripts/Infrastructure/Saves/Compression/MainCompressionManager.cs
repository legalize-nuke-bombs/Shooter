using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shooter.Infrastructure
{
    public static class MainCompressionManager
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private static Dictionary<string, CompressionManager> managersByKey;
        private static Dictionary<string, CompressionManager> managersByExtension;
        private static List<CompressionManager> knownManagers;

        public static IEnumerable<string> Keys
        {
            get
            {
                DiscoverManagers();
                return knownManagers.Select(manager => manager.Key);
            }
        }

        public static string CompressFolder(string folderPath)
        {
            DiscoverManagers();

            string algorithm = Config.GetGameConfig().Server.SaveCompressionAlgorithm.ToLowerInvariant();
            if (!managersByKey.TryGetValue(algorithm, out CompressionManager manager))
            {
                Log.Warn($"No compression manager for '{algorithm}', {folderPath} stays as is");
                return folderPath;
            }

            Log.Info($"{folderPath} will be stored with {manager.Key}");
            return manager.CompressFolder(folderPath);
        }

        public static byte[] ReadFile(string archivePath, string fileName)
        {
            CompressionManager manager = FindManagerForArchive(archivePath);
            return manager == null ? null : manager.ReadFile(archivePath, fileName);
        }

        public static void DeleteArchive(string archivePath)
        {
            FindManagerForArchive(archivePath)?.DeleteArchive(archivePath);
        }

        private static CompressionManager FindManagerForArchive(string archivePath)
        {
            DiscoverManagers();

            string extension = Directory.Exists(archivePath) ? "" : Path.GetExtension(archivePath).ToLowerInvariant();
            if (managersByExtension.TryGetValue(extension, out CompressionManager manager)) return manager;

            Log.Warn($"No compression manager for '{extension}' of {archivePath}");
            return null;
        }

        private static void DiscoverManagers()
        {
            if (knownManagers != null) return;

            managersByKey = new Dictionary<string, CompressionManager>();
            managersByExtension = new Dictionary<string, CompressionManager>();
            knownManagers = new List<CompressionManager>();

            IEnumerable<Type> types = typeof(CompressionManager).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && typeof(CompressionManager).IsAssignableFrom(type))
                .OrderBy(type => type.Name, StringComparer.Ordinal);

            foreach (Type type in types)
            {
                CompressionManager manager = (CompressionManager)Activator.CreateInstance(type);
                string normalizedKey = manager.Key.ToLowerInvariant();
                string normalizedExtension = manager.Extension.ToLowerInvariant();
                if (managersByKey.ContainsKey(normalizedKey) || managersByExtension.ContainsKey(normalizedExtension))
                {
                    Log.Error($"Compression manager {type.Name} duplicates an already known one ({normalizedKey} - {normalizedExtension})");
                    continue;
                }

                managersByKey.Add(normalizedKey, manager);
                managersByExtension.Add(normalizedExtension, manager);
                knownManagers.Add(manager);
            }

            Log.Info($"{knownManagers.Count} compression managers are known");
        }
    }
}
