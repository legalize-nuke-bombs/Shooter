using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public class SaveManager : MonoBehaviour
    {
        private const string Extension = ".json";

        private const string StampFormat = "yyyy-MM-dd_HH-mm-ss";

        private static readonly Journal Log = Logs.Here();

        private static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        private class WorldSnapshot
        {
            public string Version { get; set; }

            public Dictionary<string, EntitySnapshot> Entities { get; set; } = new();
        }

        private class EntitySnapshot
        {
            public string PrefabKey { get; set; }

            public JObject Components { get; set; }
        }

        private Register<SaveableObject> saveables;

        private void Awake()
        {
            saveables = Registers.Current.Of<SaveableObject>();
        }

        private static string Location()
        {
            string stamp = DateTime.Now.ToString(StampFormat, CultureInfo.InvariantCulture);

            return Path.Combine(Config.Root(), Config.Read().Server.SavesFolder, stamp + Extension);
        }

        private static void Write(string path, WorldSnapshot snapshot)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(snapshot, Settings));
                Log.Info($"World saved into {path}: {snapshot.Entities.Count} entities");
            }
            catch (Exception e)
            {
                Log.Error($"World {path} can not be written: {e.Message}");
            }
        }

        public void Save()
        {
            var serializer = JsonSerializer.Create(Settings);
            var snapshot = new WorldSnapshot { Version = Application.version };

            foreach (SaveableObject saveable in saveables.All)
            {
                if (string.IsNullOrEmpty(saveable.Id))
                {
                    Log.Warn($"Entity {saveable.name} has no id and stays out of the world save");
                    continue;
                }

                var entity = new EntitySnapshot
                {
                    PrefabKey = saveable.PrefabKey,
                    Components = JObject.FromObject(saveable.Save(), serializer)
                };

                if (!snapshot.Entities.TryAdd(saveable.Id, entity))
                {
                    Log.Warn($"Entity {saveable.name} shares id {saveable.Id} with an entity already saved");
                }
            }

            Write(Location(), snapshot);
        }

        public void Load()
        {
        }
    }
}
