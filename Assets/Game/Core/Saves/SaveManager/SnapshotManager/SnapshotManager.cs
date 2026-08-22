using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public Snapshot Build()
        {
            Log.Info($"Entity {name} is building snapshot...");
            var snapshot = new Snapshot
            {
                GameObjects = new Dictionary<string, JObject>()
            };

            SaveableObject[] saveables = FindObjectsByType<SaveableObject>(FindObjectsInactive.Include);
            foreach (SaveableObject saveable in saveables)
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
                    saveableData = JObject.FromObject(saveableDatRaw, SaveJson.Serializer);
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
                File.WriteAllText(path, JsonConvert.SerializeObject(snapshot, SaveJson.Settings));
                Log.Info($"Entity {name} wrote snapshot into {path}: {snapshot.GameObjects.Count} entities");
            }
            catch (Exception e)
            {
                Log.Error($"Entity {name} failed to wrote snapshot into {path}: {e.Message}");
            }
        }

        public void Load(byte[] bytes)
        {
            Log.Info($"Entity {name} is loading from snapshot...");

            Snapshot snapshot;
            try
            {
                using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, true);
                snapshot = JsonConvert.DeserializeObject<Snapshot>(reader.ReadToEnd(), SaveJson.Settings);
            }
            catch (Exception e)
            {
                Log.Error($"Entity {name} failed to decode snapshot, world will not be loaded: {e.Message}");
                return;
            }
            Log.Info($"Entity {name} decoded snapshot, snapshot game object count {snapshot.GameObjects.Count}");

            var dict = new Dictionary<string, SaveableObject>();
            foreach (SaveableObject saveableObject in FindObjectsByType<SaveableObject>(FindObjectsInactive.Include))
            {
                if (!saveableObject.TryGetComponent(out GameObjectId saveableObjectId))
                {
                    Log.Warn($"Entity {name} found saveable object {saveableObject.name} with not id");
                    continue;
                }
                if (!dict.TryAdd(saveableObjectId.Id, saveableObject))
                {
                    Log.Warn($"Entity {name} found saveable object {saveableObject.name} with id duplicate: {saveableObjectId.Id}");
                    continue;
                }
            }
            Log.Info($"Entity {name} found {dict.Count} in scene saveable objects");

            int inSceneOk = 0;
            int inSceneFailed = 0;
            int nonSceneOk = 0;
            int nonSceneFailed = 0;
            foreach (var kvp in snapshot.GameObjects)
            {
                string targetId = kvp.Key;
                JObject targetPayload = kvp.Value;

                if (dict.TryGetValue(targetId, out SaveableObject target))
                {
                    Log.Info($"Entity {name} is loading {target.name} {targetId}...");
                    try
                    {
                        target.Load(targetPayload);
                        inSceneOk++;
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Entity {name} failed to load {target.name} {targetId} : {e.Message}");
                        inSceneFailed++;
                    }
                }
                else
                {
                    Log.Info($"Entity {name} found non-scene object {targetId}");
                    // TODO Spawn
                    nonSceneOk++;
                }
            }
            Log.Info($"Entity {name} loaded from snapshot, inSceneOk {inSceneOk} inSceneFailed {inSceneFailed} nonSceneOk {nonSceneOk} nonSceneFailed {nonSceneFailed}");
        }
    }
}
