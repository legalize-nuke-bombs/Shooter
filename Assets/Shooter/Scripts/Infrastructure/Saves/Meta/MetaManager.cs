using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public static class MetaManager
    {
        private static readonly Journal Log = Logs.Here();

        public static Meta BuildMeta()
        {
            Log.Info("Building meta...");
            return new Meta()
            {
                Version = Application.version,
                Stamp = DateTime.Now
            };
        }

        public static void WriteMeta(string path, Meta meta)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(meta, Meta.JsonSettings));
                Log.Info($"Meta is written into {path}");
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to write meta into {path}: {e.Message}");
            }
        }
    }
}
