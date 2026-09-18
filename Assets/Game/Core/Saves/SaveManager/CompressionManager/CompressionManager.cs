using System;
using System.IO;
using Shooter.Logging;

namespace Shooter.Game.Core.Saves
{
    public abstract class CompressionManager
    {
        private static readonly Journal Log = Logs.Here();

        public abstract string Key { get; }
        public abstract string Extension { get; }

        private string Name => GetType().Name;

        public string Compress(string path)
        {
            string target = path + Extension;
            Log.Info($"{Name} is storing {path} as {target}...");
            try
            {
                CompressRaw(path);
            }
            catch (Exception e)
            {
                Log.Info($"{Name} failed to store {path} as {target}: {e.Message}");
                return path;
            }

            Log.Info($"{Name} successfully stored {path} as {target}");

            if (target != path) Cleanup(path);
            return target;
        }

        public byte[] Read(string location, string file)
        {
            try
            {
                return ReadRaw(location, file);
            }
            catch (Exception e)
            {
                Log.Warn($"{Name} failed to read {file} from {location}: {e.Message}");
                return null;
            }
        }

        public void Delete(string location)
        {
            try
            {
                DeleteRaw(location);
                Log.Info($"{Name} deleted {location}");
            }
            catch (Exception e)
            {
                Log.Error($"{Name} failed to delete {location}: {e.Message}");
            }
        }

        private void Cleanup(string path)
        {
            try
            {
                Directory.Delete(path, true);
                Log.Info($"{Name} successfully deleted {path}");
            }
            catch (Exception e)
            {
                Log.Info($"{Name} failed to delete {path}: {e.Message}");
            }
        }

        protected abstract void CompressRaw(string path);

        protected abstract byte[] ReadRaw(string location, string file);

        protected virtual void DeleteRaw(string location)
        {
            File.Delete(location);
        }
    }
}
