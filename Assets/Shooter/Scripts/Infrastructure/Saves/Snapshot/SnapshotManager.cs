using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public static class SnapshotManager
    {
        private static readonly Journal Log = Logs.Here();

        public static Snapshot BuildSnapshot()
        {
            Log.Info("Building snapshot...");
            Snapshot snapshot = new()
            {
                GameObjects = new Dictionary<string, object>()
            };

            TArray.View<SaveableEntity> saveables = Registers.Instance.GetAll<SaveableEntity>(RegisterInactivePolicy.Include);
            foreach (SaveableEntity saveable in saveables)
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

        public static bool TryWriteSnapshot(string path, Snapshot snapshot)
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

        public static bool TryLoadSnapshot(FrozenWorld frozenWorld, byte[] snapshotBytes)
        {
            Log.Info("Loading from snapshot...");

            Snapshot snapshot;
            try
            {
                using StreamReader reader = new(new MemoryStream(snapshotBytes), Encoding.UTF8, true);
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
            var loadedEntities = new List<SaveableEntity>();
            var componentSaves = new List<ComponentSave>();
            foreach (KeyValuePair<string, object> record in snapshot.GameObjects)
            {
                string targetId = record.Key;
                SaveToken targetValue = SaveToken.FromObject(record.Value);

                if (frozenWorld.TryGetSaveableEntity(targetId, out SaveableEntity target))
                {
                    Log.Info($"Loading {target.name} {targetId}...");
                    try
                    {
                        target.LoadEntity(targetValue, componentSaves);
                        loadedEntities.Add(target);
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
                        if (SaveableEntity.TrySpawnFromSave(frozenWorld, targetId, targetValue, componentSaves, out SaveableEntity spawned))
                        {
                            loadedEntities.Add(spawned);
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

            int componentsLoaded = 0;
            foreach (ComponentSave componentSave in componentSaves.OrderBy(save => ExecutionOrderOf(save.Component)))
            {
                try
                {
                    componentSave.Component.LoadObject(componentSave.Content);
                    componentsLoaded++;
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {componentSave.Entity.name} failed to load component {componentSave.Component.GetType().Name}: {e.Message}");
                }
            }

            var postLoadables = new List<(SaveableEntity Entity, ISaveableComponent Component)>();
            foreach (SaveableEntity loadedEntity in loadedEntities)
            {
                if (!loadedEntity.IsSpawned) continue;

                foreach (ISaveableComponent component in loadedEntity.GetComponents<ISaveableComponent>())
                {
                    postLoadables.Add((loadedEntity, component));
                }
            }

            int postLoaded = 0;
            foreach ((SaveableEntity entity, ISaveableComponent component) in postLoadables.OrderBy(postLoadable => ExecutionOrderOf(postLoadable.Component)))
            {
                try
                {
                    component.PostLoad();
                    postLoaded++;
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {entity.name} failed to post-load component {component.GetType().Name}: {e.Message}");
                }
            }

            Log.Info($"Loaded from snapshot, inSceneOk {inSceneOk} inSceneFailed {inSceneFailed} nonSceneOk {nonSceneOk} nonSceneFailed {nonSceneFailed} componentsLoaded {componentsLoaded} postLoaded {postLoaded}");
            return true;
        }

        private static int ExecutionOrderOf(ISaveableComponent component)
        {
            DefaultExecutionOrder attribute = component.GetType().GetCustomAttribute<DefaultExecutionOrder>(true);
            return attribute == null ? 0 : attribute.order;
        }
    }
}
