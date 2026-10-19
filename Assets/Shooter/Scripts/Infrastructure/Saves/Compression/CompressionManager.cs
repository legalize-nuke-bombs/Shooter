using System;
using System.IO;

namespace Shooter.Infrastructure
{
    public abstract class CompressionManager
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        public abstract string Key { get; }
        public abstract string Extension { get; }

        private string TypeName => GetType().Name;

        public string CompressFolder(string folderPath)
        {
            string archivePath = folderPath + Extension;
            Log.Info($"{TypeName} is storing {folderPath} as {archivePath}...");
            try
            {
                CompressFolderRaw(folderPath);
            }
            catch (Exception e)
            {
                Log.Warn($"{TypeName} failed to store {folderPath} as {archivePath}: {e.Message}");
                return folderPath;
            }

            Log.Info($"{TypeName} successfully stored {folderPath} as {archivePath}");

            if (archivePath != folderPath) DeleteSourceFolder(folderPath);
            return archivePath;
        }

        public byte[] ReadFile(string archivePath, string fileName)
        {
            try
            {
                return ReadFileRaw(archivePath, fileName);
            }
            catch (Exception e)
            {
                Log.Warn($"{TypeName} failed to read {fileName} from {archivePath}: {e.Message}");
                return null;
            }
        }

        public void DeleteArchive(string archivePath)
        {
            try
            {
                DeleteArchiveRaw(archivePath);
                Log.Info($"{TypeName} deleted {archivePath}");
            }
            catch (Exception e)
            {
                Log.Warn($"{TypeName} failed to delete {archivePath}: {e.Message}");
            }
        }

        private void DeleteSourceFolder(string folderPath)
        {
            try
            {
                Directory.Delete(folderPath, true);
                Log.Info($"{TypeName} successfully deleted {folderPath}");
            }
            catch (Exception e)
            {
                Log.Warn($"{TypeName} failed to delete {folderPath}: {e.Message}");
            }
        }

        protected abstract void CompressFolderRaw(string folderPath);

        protected abstract byte[] ReadFileRaw(string archivePath, string fileName);

        protected virtual void DeleteArchiveRaw(string archivePath)
        {
            File.Delete(archivePath);
        }
    }
}
