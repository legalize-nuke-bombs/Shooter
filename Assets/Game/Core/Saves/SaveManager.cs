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

        private static string Folder(string worldName)
        {
            return Path.Combine(Config.Root(), Config.Read().Server.SavesFolder, worldName);
        }

        private static string Stamped()
        {
            return DateTime.Now.ToString(StampFormat, CultureInfo.InvariantCulture) + Extension;
        }

        private static bool Write(string path, WorldSnapshot snapshot)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(snapshot, Settings));
                Log.Info($"World saved into {path}: {snapshot.Entities.Count} entities");

                return true;
            }
            catch (Exception e)
            {
                Log.Error($"World {path} can not be written: {e.Message}");

                return false;
            }
        }

        private static void Sweep(string folder, int kept)
        {
            string[] saves = Directory.GetFiles(folder, "*" + Extension);
            int extra = saves.Length - Math.Max(kept, 1);
            if (extra <= 0) return;

            Array.Sort(saves, StringComparer.Ordinal);

            for (var i = 0; i < extra; i++)
            {
                try
                {
                    File.Delete(saves[i]);
                }
                catch (Exception e)
                {
                    Log.Warn($"Old save {saves[i]} can not be deleted: {e.Message}");
                }
            }

            Log.Info($"Swept {extra} old saves of {saves.Length} in {folder}, keeping {kept}");
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

            string folder = Folder(worldName);
            if (Write(Path.Combine(folder, Stamped()), snapshot))
            {
                Sweep(folder, Config.Read().Server.SavesKept);
            }
        }

        public void Load(string worldName)
        {
        }
    }
}
