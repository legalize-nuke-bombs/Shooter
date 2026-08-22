using System;
using System.IO;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public abstract class CompressionManager : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public string Compress(string path)
        {
            string target = path + Extension;
            Log.Info($"Entity {name} is storing {path} as {target}...");
            try
            {
                CompressRaw(path);
            }
            catch (Exception e)
            {
                Log.Info($"Entity {name} failed to store {path} as {target}: {e.Message}");
                return path;
            }

            Log.Info($"Entity {name} successfully stored {path} as {target}");

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
                Log.Warn($"Entity {name} failed to read {file} from {location}: {e.Message}");
                return null;
            }
        }

        public void Delete(string location)
        {
            try
            {
                DeleteRaw(location);
                Log.Info($"Entity {name} deleted {location}");
            }
            catch (Exception e)
            {
                Log.Error($"Entity {name} failed to delete {location}: {e.Message}");
            }
        }

        private void Cleanup(string path)
        {
            try
            {
                Directory.Delete(path, true);
                Log.Info($"Entity {name} successfully deleted {path}");
            }
            catch (Exception e)
            {
                Log.Info($"Entity {name} failed to delete {path}: {e.Message}");
            }
        }

        protected abstract void CompressRaw(string path);

        protected abstract byte[] ReadRaw(string location, string file);

        protected virtual void DeleteRaw(string location)
        {
            File.Delete(location);
        }

        public abstract string Key { get; }
        public abstract string Extension { get; }
    }
}
