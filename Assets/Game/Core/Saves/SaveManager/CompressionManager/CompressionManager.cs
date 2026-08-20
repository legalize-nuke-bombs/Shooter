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
            Log.Info($"Entity {name} is compressing {path}...");
            bool ok = true;
            try
            {
                CompressRaw(path);
            }
            catch (Exception e)
            {
                Log.Info($"Entity {name} failed to compress {path}: {e.Message}");
                ok = false;
            }

            if (!ok)
            {
                return path;
            }
            Log.Info($"Entity {name} successfully compressed {path}");

            Cleanup(path);
            return path + Extension;
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

        public abstract string Key { get; }
        public abstract string Extension { get; }
    }
}
