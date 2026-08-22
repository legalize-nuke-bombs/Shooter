using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shooter.Configuring;
using Shooter.Logging;

namespace Shooter.Game.Core.Saves
{
    public static class SaveLibrary
    {
        public const string Folder = "Saves";
        private static readonly Journal Log = Logs.Here();

        public static string Location => Path.Combine(Config.Root(), Folder);

        public static List<SaveEntry> All()
        {
            var entries = new List<SaveEntry>();
            string root = Location;
            if (!Directory.Exists(root)) return entries;

            MainCompressionManager compression = MainCompressionManager.Current;
            if (compression == null)
            {
                Log.Error($"No compression manager, the library at {root} stays unread");
                return entries;
            }

            foreach (string location in Directory.EnumerateFileSystemEntries(root))
            {
                CompressionManager manager = compression.Resolve(location);
                if (manager == null)
                {
                    Log.Warn($"Save {location} has no compression manager for its extension, skipped");
                    continue;
                }

                SaveEntry entry = SaveEntry.Read(location, manager);
                if (entry != null) entries.Add(entry);
            }

            entries.Sort((left, right) => right.Meta.Stamp.CompareTo(left.Meta.Stamp));

            return entries;
        }

        public static SaveEntry Latest()
        {
            return All().FirstOrDefault();
        }

        public static void Delete(SaveEntry entry)
        {
            entry.Manager.Delete(entry.Location);
        }
    }
}
