using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Shooter.Infrastructure
{
    public static class MetaManager
    {
        private static readonly Journal Log = Logs.Here();

        public static Meta Build()
        {
            Log.Info("Building meta...");
            return new Meta()
            {
                Version = Application.version,
                Stamp = DateTime.Now
            };
        }

        public static void Write(string path, Meta meta)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(meta, Meta.Json));
                Log.Info($"Meta is written into {path}");
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to write meta into {path}: {e.Message}");
            }
        }
    }
}
