using System;
using System.IO;
using Shooter.Logging;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shooter.Game.World;

namespace Shooter.Game.Core.Saves
{
    public class MetaManager : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        };

        public Meta Build()
        {
            Log.Info($"Entity {name} is building meta...");
            return new Meta()
            {
                Version = Application.version,
                Stamp = DateTime.Now,
                Clock = Clock.Current.DateTime()
            };
        }

        public void Write(string path, Meta meta)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(meta, Settings));
                Log.Info($"Entity {name} wrote meta into {path}");
            }
            catch (Exception e)
            {
                Log.Error($"Entity {name} failed to wrote meta into {path}: {e.Message}");
            }
        }
    }
}
