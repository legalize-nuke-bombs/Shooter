using System;
using System.Collections.Generic;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    [RequireComponent(typeof(GameObjectId))]
    [RequireComponent(typeof(NetworkObject))]
    public class SaveableObject : NetworkBehaviour, ISaveable
    {
        private static readonly Journal Log = Logs.Here();

        private GameObjectId id;
        private NetworkObject networkObject;

        public string Id => id.Id;
        private string Metadata
        {
            get => name;
            set => name = value;
        }
        private bool Static => networkObject.InScenePlaced;
        private bool Dynamic => !networkObject.InScenePlaced;
        private bool Spawned => networkObject.IsSpawned;
        private string PrefabId()
        {
            SaveablePrefabCatalog catalog = Catalogs.Of<SaveablePrefabCatalog>();
            FixedString32Bytes result = catalog.PrefabId(networkObject.PrefabIdHash);
            if (result == null)
            {
                Log.Error($"Entity {name} is not registered as saveable prefab!");
                return null;
            }
            return result.ToString();
        }

        private void Awake()
        {
            id = GetComponent<GameObjectId>();
            networkObject = GetComponent<NetworkObject>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            if (!string.IsNullOrEmpty(id.Id) || NetworkObject.InScenePlaced) return;
            id.Assign(Guid.NewGuid().ToString());
            Log.Info($"Entity {name} got dynamic save id {id.Id}");
        }

        private struct SaveData
        {
            public string Metadata { get; set; }
            public bool Spawned { get; set; }
            public string PrefabId { get; set; }
            public Dictionary<string, SaveToken> Components { get; set; }
        }

        public object SaveObject()
        {
            Log.Info($"Entity {name} is saving...");

            if (Dynamic && !Spawned)
            {
                Log.Warn($"Entity {name} is not valid, ignoring");
                return null;
            }

            var sd = new SaveData()
            {
                Metadata = Metadata,
                Spawned = Spawned,
                PrefabId = PrefabId(),
                Components = new Dictionary<string, SaveToken>()
            };

            ISaveableComponent[] saveables = Spawned ? GetComponents<ISaveableComponent>() : Array.Empty<ISaveableComponent>();
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

                if (!sd.Components.TryAdd(saveableKey, saveableData))
                {
                    Log.Warn($"Entity {name} found duplicate key {saveableKey}");
                }
            }

            Log.Info($"Entity {name} saved {sd.Components.Count} components, spawned = {Spawned}");
            return sd;
        }

        public void LoadObject(SaveToken content)
        {
            Log.Info($"Entity {name} is loading...");

            SaveData sd = content.To<SaveData>();
            Metadata = sd.Metadata;

            if (Dynamic && !sd.Spawned)
            {
                Log.Warn($"Entity {name} is not valid");
            }
            if (!sd.Spawned)
            {
                if (sd.Components.Count > 0)
                {
                    Log.Warn($"Entity {name} is not spawned, but has {sd.Components.Count} serialized components, ignoring");
                }
                Log.Info($"Entity {name} is not spawned, despawning...");
                networkObject.Despawn(Dynamic);
                return;
            }

            ISaveableComponent[] saveables = GetComponents<ISaveableComponent>();
            var known = new HashSet<string>();
            foreach (ISaveableComponent saveable in saveables)
            {
                string saveableKey = saveable.ComponentKey;
                known.Add(saveableKey);

                if (!sd.Components.TryGetValue(saveableKey, out SaveToken componentData))
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

            foreach (string key in sd.Components.Keys)
            {
                if (!known.Contains(key))
                {
                    Log.Warn($"Entity {name} found unknown saved property {key}");
                }
            }

            Log.Info($"Entity {name} loaded {known.Count} / {saveables.Length} components (provided {sd.Components.Count})");
        }

        public static void Spawn(FrozenWorld world, string id, SaveToken content)
        {
            SaveData saveData = content.To<SaveData>();
            if (!saveData.Spawned)
            {
                Log.Warn($"Entity id {id} is not valid, ignoring");
                return;
            }
            string prefabId = saveData.PrefabId;
            if (String.IsNullOrEmpty(prefabId))
            {
                throw new ArgumentException("Serialized prefab id is null or empty");
            }
            SaveablePrefabCatalog catalog = Catalogs.Of<SaveablePrefabCatalog>();
            GameObject prefab = catalog.Of(prefabId).Prefab;
            if (prefab == null)
            {
                throw new ArgumentException($"Failed to find prefab {prefabId}");
            }

            GameObject body = Spawner.Current.Spawn(prefab);
            if (body.TryGetComponent(out GameObjectId gameObjectId))
            {
                gameObjectId.Assign(id);
            }
            else
            {
                Log.Warn($"Prefab {prefabId} does not have game object id");
            }

            if (body.TryGetComponent(out SaveableObject saveableObject))
            {
                world.Adopt(saveableObject);
                saveableObject.LoadObject(content);
            }
            else
            {
                Log.Warn($"Prefab {prefabId} does not have saveable object");
            }
        }
    }
}
