using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public static class SnapshotManager
    {
        private static readonly Journal Log = Logs.Here();

        public static Snapshot Build()
        {
            Log.Info("Building snapshot...");
            Snapshot snapshot = new()
            {
                GameObjects = new Dictionary<string, object>()
            };

            SaveableObject[] saveables = UnityEngine.Object.FindObjectsByType<SaveableObject>(FindObjectsInactive.Include);
            foreach (SaveableObject saveable in saveables)
            {
                if (string.IsNullOrEmpty(saveable.Id))
                {
                    Log.Warn($"Snapshot found {saveable.name} with empty id");
                    continue;
                }

                object saved;
                try
                {
                    saved = saveable.SaveObject();
                }
                catch (Exception e)
                {
                    Log.Warn($"Failed to save {saveable.name} {saveable.Id} : {e.Message}");
                    continue;
                }

                if (saved == null) continue;

                if (!snapshot.GameObjects.TryAdd(saveable.Id, saved))
                {
                    Log.Warn($"Snapshot found that {saveable.name} shares id {saveable.Id} with an entity already saved");
                }
            }

            Log.Info("Snapshot is built");
            return snapshot;
        }

        public static bool Write(string path, Snapshot snapshot)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(snapshot, SaveJson.Settings));
                Log.Info($"Snapshot is written into {path}: {snapshot.GameObjects.Count} entities");
                return true;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to write snapshot into {path}: {e.Message}");
                return false;
            }
        }

        public static bool Load(FrozenWorld world, byte[] bytes)
        {
            Log.Info("Loading from snapshot...");

            Snapshot snapshot;
            try
            {
                using StreamReader reader = new(new MemoryStream(bytes), Encoding.UTF8, true);
                snapshot = JsonConvert.DeserializeObject<Snapshot>(reader.ReadToEnd(), SaveJson.Settings);
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to decode snapshot, world will not be loaded: {e.Message}");
                return false;
            }

            Log.Info($"Snapshot is decoded, snapshot game object count {snapshot.GameObjects.Count}");

            int inSceneOk = 0;
            int inSceneFailed = 0;
            int nonSceneOk = 0;
            int nonSceneFailed = 0;
            foreach (KeyValuePair<string, object> record in snapshot.GameObjects)
            {
                string targetId = record.Key;
                SaveToken targetValue = SaveToken.From(record.Value);

                if (world.TryGet(targetId, out SaveableObject target))
                {
                    Log.Info($"Loading {target.name} {targetId}...");
                    try
                    {
                        target.LoadObject(targetValue);
                        inSceneOk++;
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Failed to load {target.name} {targetId} : {e.Message}");
                        inSceneFailed++;
                    }
                }
                else
                {
                    Log.Info($"Spawning non-scene object {targetId}...");
                    try
                    {
                        if (SaveableObject.Spawn(world, targetId, targetValue))
                        {
                            nonSceneOk++;
                        }
                        else
                        {
                            nonSceneFailed++;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Failed to spawn {targetId} : {e.Message}");
                        nonSceneFailed++;
                    }
                }
            }

            Log.Info($"Loaded from snapshot, inSceneOk {inSceneOk} inSceneFailed {inSceneFailed} nonSceneOk {nonSceneOk} nonSceneFailed {nonSceneFailed}");
            return true;
        }
    }
}
