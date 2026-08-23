using System;
using System.Collections.Generic;
using Shooter.Game.Core.GameObject;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(GameObjectId))]
    public class SaveableObject : NetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private bool spawned = true;
        private string prefabId;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            AssignId();
            AssignPrefabId();
        }

        private void AssignId()
        {
            GameObjectId id = GetComponent<GameObjectId>();
            if (!string.IsNullOrEmpty(id.Id) || NetworkObject.InScenePlaced) return;
            id.Assign(Guid.NewGuid().ToString());
            Log.Info($"Entity {name} got dynamic save id {id.Id}");
        }

        private void AssignPrefabId()
        {
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject.InScenePlaced) return;
            SaveablePrefabCatalog catalog = Catalogs.Of<SaveablePrefabCatalog>();
            FixedString32Bytes result = catalog.PrefabId(networkObject.PrefabIdHash);
            if (result == null)
            {
                Log.Error($"Entity {name} is not registered as saveable prefab!");
                return;
            }
            Log.Info($"Entity {name} got prefab id `{result}`");
            prefabId = result.ToString();
        }

        public override void OnNetworkDespawn()
        {
            spawned = false;
        }

        private const string MainComponentKey = "SaveableObject";
        public string ComponentKey => MainComponentKey;

        private struct SaveData
        {
            public bool Spawned { get; set; }
            public string PrefabId { get; set; }
            public string Metadata { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Spawned = spawned,
                PrefabId = prefabId,
                Metadata = name
            };
        }

        public void LoadObject(SaveToken content)
        {
            spawned = content.To<SaveData>().Spawned;
        }

        public Dictionary<string, SaveToken> Save()
        {
            Log.Info($"Entity {name} is saving...");

            ISaveableComponent[] saveables = GetComponents<ISaveableComponent>();
            var components = new Dictionary<string, SaveToken>();
            foreach (ISaveableComponent saveable in saveables)
            {
                string saveableKey = saveable.ComponentKey;
                SaveToken saveableData;
                try
                {
                    object saved = saveable.SaveObject();
                    if (saved == null) continue;

                    saveableData = SaveToken.From(saved);
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {name} failed to save component {saveableKey}: {e.Message}");
                    continue;
                }

                if (!components.TryAdd(saveableKey, saveableData))
                    Log.Warn($"Entity {name} found duplicate key {saveableKey}");
            }

            Log.Info($"Entity {name} saved {components.Count} components, spawned = {spawned}");
            return components;
        }

        public void Load(Dictionary<string, SaveToken> components)
        {
            Log.Info($"Entity {name} is loading...");

            ISaveableComponent[] saveables = GetComponents<ISaveableComponent>();
            var known = new HashSet<string>();
            foreach (ISaveableComponent saveable in saveables)
            {
                string saveableKey = saveable.ComponentKey;
                known.Add(saveableKey);

                if (!components.TryGetValue(saveableKey, out SaveToken componentData))
                {
                    Log.Warn($"Entity {name} failed to find saved data for {saveableKey}");
                    continue;
                }

                try
                {
                    saveable.LoadObject(componentData);
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {name} failed to load component {saveableKey}: {e.Message}");
                }
            }

            foreach (string key in components.Keys)
                if (!known.Contains(key))
                    Log.Warn($"Entity {name} found unknown saved property {key}");

            Log.Info($"Entity {name} loaded {known.Count} / {saveables.Length} components (provided {components.Count}), spawned = {spawned}");
            if (!spawned) NetworkObject.Despawn(false);
        }

        public static void Spawn(FrozenWorld world, string id, Dictionary<string, SaveToken> components)
        {
            if (!components.TryGetValue(MainComponentKey, out SaveToken mainSt))
            {
                throw new ArgumentException($"Failed to find main component {MainComponentKey} in provided components");
            }
            SaveData mainSd = mainSt.To<SaveData>();
            string prefabId = mainSd.PrefabId;
            if (String.IsNullOrEmpty(prefabId))
            {
                throw new ArgumentException("Serialized prefab id is null or empty");
            }
            SaveablePrefabCatalog catalog = Catalogs.Of<SaveablePrefabCatalog>();
            UnityEngine.GameObject prefab = catalog.Of(prefabId).Prefab;
            if (prefab == null)
            {
                throw new ArgumentException($"Failed to find prefab {prefabId}");
            }

            prefab = Instantiate(prefab);
            if (prefab.TryGetComponent(out GameObjectId gameObjectId))
            {
                gameObjectId.Assign(id);
            }
            else
            {
                Log.Warn($"Prefab {prefabId} does not have game object id");
            }

            if (prefab.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn();
            }
            else
            {
                Log.Warn($"Prefab {prefabId} does not have network object");
            }

            if (prefab.TryGetComponent(out SaveableObject saveableObject))
            {
                world.Adopt(saveableObject);
                saveableObject.Load(components);
            }
            else
            {
                Log.Warn($"Prefab {prefabId} does not have saveable object");
            }
        }
    }
}
