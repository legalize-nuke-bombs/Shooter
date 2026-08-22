using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
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
                GameObjects = new Dictionary<string, Dictionary<string, SaveToken>>()
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

                if (!snapshot.GameObjects.TryAdd(saveableId.Id, saveable.Save()))
                    Log.Warn($"Entity {name} found that {saveable.name} shares id {saveableId.Id} with an entity already saved");
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

        public bool Load(FrozenWorld world, byte[] bytes)
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
                Log.Warn($"Entity {name} failed to decode snapshot, world will not be loaded: {e.Message}");
                return false;
            }

            Log.Info($"Entity {name} decoded snapshot, snapshot game object count {snapshot.GameObjects.Count}");

            int inSceneOk = 0;
            int inSceneFailed = 0;
            int nonSceneOk = 0;
            int nonSceneFailed = 0;
            foreach (KeyValuePair<string, Dictionary<string, SaveToken>> record in snapshot.GameObjects)
            {
                string targetId = record.Key;
                if (world.TryGet(targetId, out SaveableObject target))
                {
                    Log.Info($"Entity {name} is loading {target.name} {targetId}...");
                    try
                    {
                        target.Load(record.Value);
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
            return true;
        }
    }
}
