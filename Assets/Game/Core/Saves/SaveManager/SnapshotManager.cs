using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shooter.Game.Core.GameObject;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public class SnapshotManager : MonoBehaviour
    {
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

        public Snapshot Build()
        {
            Log.Info($"Entity {name} is building snapshot...");
            var serializer = JsonSerializer.Create(SnapshotManager.Settings);
            var snapshot = new Snapshot
            {
                Version = Application.version,
                Stamp = DateTime.Now,
                GameObjects = new Dictionary<string, JObject>()
            };

            foreach (SaveableObject saveable in saveables.All)
            {
                if (!saveable.TryGetComponent(out GameObjectId saveableId))
                {
                    Log.Warn($"Entity {name} found {saveable.name} with no id");
                    continue;
                }

                if (string.IsNullOrEmpty(saveableId.Id))
                {
                    Log.Warn($"Entity {name} found {saveable.name} with empty id");
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
                    Log.Warn($"Entity {name} found that {saveable.name} can not be serialized: {e.Message}");
                }
                if (saveableData == null)
                {
                    continue;
                }

                if (!snapshot.GameObjects.TryAdd(saveableId.Id, saveableData))
                {
                    Log.Warn($"Entity {name} found that {saveable.name} shares id {saveableId.Id} with an entity already saved");
                    continue;
                }
            }

            Log.Info($"Entity {name} built snapshot");
            return snapshot;
        }

        public void Write(string path, Snapshot snapshot)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(snapshot, SnapshotManager.Settings));
                Log.Info($"Entity {name} wrote snapshot into {path}: {snapshot.GameObjects.Count} entities");
            }
            catch (Exception e)
            {
                Log.Error($"Entity {name} failed to wrote snapshot into {path}: {e.Message}");
            }
        }
    }
}
