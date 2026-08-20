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
            Log.Info($"Compressing {path}...");
            bool ok = true;
            try
            {
                CompressRaw(path);
            }
            catch (Exception e)
            {
                Log.Info($"Failed to compress {path}: {e.Message}");
                ok = false;
            }

            if (!ok)
            {
                return path;
            }
            Log.Info($"Successfully compressed {path}");

            Cleanup(path);
            return path + Extension;
        }

        private void Cleanup(string path)
        {
            try
            {
                Directory.Delete(path, true);
                Log.Info($"Successfully deleted {path}");
            }
            catch (Exception e)
            {
                Log.Info($"Failed to delete {path}: {e.Message}");
            }
        }

        protected abstract void CompressRaw(string path);

        public abstract string Key { get; }
        public abstract string Extension { get; }
    }
}
