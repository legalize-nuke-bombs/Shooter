using System;
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
        private const string ZipExtension = ".zip";
        private static readonly Journal Log = Logs.Here();

        public static string Location => Path.Combine(Config.Root(), Folder);

        public static List<SaveEntry> All()
        {
            var entries = new List<SaveEntry>();
            string root = Location;
            if (!Directory.Exists(root)) return entries;

            foreach (string zip in Directory.GetFiles(root, "*" + ZipExtension)) Add(entries, zip, true);

            foreach (string folder in Directory.GetDirectories(root))
            {
                if (File.Exists(folder + ZipExtension)) continue;

                Add(entries, folder, false);
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
            try
            {
                if (entry.Compressed) File.Delete(entry.Location);
                else Directory.Delete(entry.Location, true);

                Log.Info($"Save {entry.Location} deleted");
            }
            catch (Exception e)
            {
                Log.Error($"Save {entry.Location} can not be deleted: {e.Message}");
            }
        }

        private static void Add(List<SaveEntry> entries, string location, bool compressed)
        {
            SaveEntry entry = SaveEntry.Read(location, compressed);
            if (entry != null) entries.Add(entry);
        }
    }
}
