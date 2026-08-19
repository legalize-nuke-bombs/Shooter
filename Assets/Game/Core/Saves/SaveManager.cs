using System;
using System.Collections.Generic;
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
        private const string FolderName = "Saves";

        private const string Extension = ".json";

        private const string TemporaryExtension = ".tmp";

        private static readonly Journal Log = Logs.Here();

        private static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        private Register<SaveableObject> saveables;

        private void Awake()
        {
            saveables = Registers.Current.Of<SaveableObject>();
        }

        public void Save(string worldName)
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

            Write(Location(worldName), snapshot);
        }

        public void Load(string worldName)
        {
        }

        private void Write(string path, WorldSnapshot snapshot)
        {
            string temporary = path + TemporaryExtension;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonConvert.SerializeObject(snapshot, Settings));
                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path);
                }
                Log.Info($"World {path} saved: {snapshot.Entities.Count} entities of {saveables.Count} registered");
            }
            catch (Exception e)
            {
                Log.Error($"World {path} can not be written: {e.Message}");
            }
        }

        private static string Location(string worldName)
        {
            return Path.Combine(Config.Root(), FolderName, worldName + Extension);
        }

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
    }
}
