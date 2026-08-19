using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shooter.Configuring;
using Shooter.Game.Core.GameObject;
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

            public Dictionary<string, JObject> Entities { get; set; } = new();
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
            Log.Info("Saving...");
            var serializer = JsonSerializer.Create(Settings);
            var snapshot = new WorldSnapshot { Version = Application.version };

            foreach (SaveableObject saveable in saveables.All)
            {
                if (!saveable.TryGetComponent(out GameObjectId saveableId))
                {
                    Log.Warn($"Entity {saveable.name} does not have id");
                    continue;
                }

                if (string.IsNullOrEmpty(saveableId.Id))
                {
                    Log.Warn($"Entity {saveable.name} id is not set");
                    continue;
                }

                JObject saveableData = null;
                try
                {
                    Dictionary<string, object> saveableDatRaw = saveable.Save();
                    saveableData = JObject.FromObject(saveableDatRaw, serializer);
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {saveable.name} can not be serialized: {e.Message}");
                }
                if (saveableData == null)
                {
                    continue;
                }

                if (!snapshot.Entities.TryAdd(saveableId.Id, saveableData))
                {
                    Log.Warn($"Entity {saveable.name} shares id {saveableId.Id} with an entity already saved");
                    continue;
                }
            }

            Write(Location(), snapshot);
        }

        public void Load()
        {
        }

        // TODO Test
        private float timer = 0;
        private float timerInterval = 10;
        public void Update()
        {
           timer += Time.deltaTime;
           if (timer >= timerInterval)
           {
               Save();
               timer = 0;
           }
        }
    }
}
