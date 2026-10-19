using System.Collections.Generic;
using System.IO;

namespace Shooter.Infrastructure
{
    public static class SaveLibrary
    {
        public const string FolderName = "Saves";

        public static string FolderPath => Path.Combine(Config.GetRootFolder(), FolderName);

        public static List<SaveEntry> ReadAllEntries()
        {
            var entries = new List<SaveEntry>();
            string folderPath = FolderPath;
            if (!Directory.Exists(folderPath)) return entries;

            foreach (string location in Directory.EnumerateFileSystemEntries(folderPath))
            {
                SaveEntry entry = SaveEntry.ReadFromLocation(location);
                if (entry != null) entries.Add(entry);
            }

            entries.Sort((left, right) => right.Meta.Stamp.CompareTo(left.Meta.Stamp));

            return entries;
        }

        public static void DeleteSave(SaveEntry entry)
        {
            MainCompressionManager.DeleteArchive(entry.Location);
        }
    }
}
