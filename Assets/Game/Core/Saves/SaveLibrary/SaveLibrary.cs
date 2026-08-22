using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shooter.Configuring;

namespace Shooter.Game.Core.Saves
{
    public static class SaveLibrary
    {
        public const string Folder = "Saves";

        public static string Location => Path.Combine(Config.Root(), Folder);

        public static List<SaveEntry> All()
        {
            var entries = new List<SaveEntry>();
            string root = Location;
            if (!Directory.Exists(root)) return entries;

            foreach (string location in Directory.EnumerateFileSystemEntries(root))
            {
                SaveEntry entry = SaveEntry.Read(location);
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
            MainCompressionManager.Current.Delete(entry.Location);
        }
    }
}
