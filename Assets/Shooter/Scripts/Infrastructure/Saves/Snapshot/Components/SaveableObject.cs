using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [RequireComponent(typeof(NetworkObject))]
    public class SaveableObject : NetworkBehaviour, ISaveable
    {
        public const string IdField = nameof(id);
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private string id;

        private NetworkObject networkObject;
        private bool activeAfterThaw = true;

        public string Id => id;

        public bool IsActiveAfterThaw => activeAfterThaw;

        private string Metadata
        {
            get => name;
            set => name = value;
        }

        private bool IsStatic => networkObject.InScenePlaced;

        private bool IsDynamic => !networkObject.InScenePlaced;

        private bool IsNetworkObjectSpawned => networkObject.IsSpawned;

        private string GetPrefabId()
        {
            if (IsStatic) return null;

            SaveablePrefabCatalog catalog = Catalogs.GetCatalog<SaveablePrefabCatalog>();
            FixedString32Bytes prefabId = catalog.GetPrefabIdByHash(networkObject.PrefabIdHash);
            if (prefabId == null)
            {
                Log.Error($"Entity {name} is not registered as saveable prefab!");
                return null;
            }

            return prefabId.ToString();
        }

        private void Awake()
        {
            networkObject = GetComponent<NetworkObject>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            if (!string.IsNullOrEmpty(id) || NetworkObject.InScenePlaced) return;

            id = Guid.NewGuid().ToString();
            Log.Info($"Entity {name} got dynamic save id {id}");
        }

        private struct SaveData
        {
            public string Metadata { get; set; }
            public bool Spawned { get; set; }
            public bool Active { get; set; }
            public string PrefabId { get; set; }
            public Dictionary<string, SaveToken> Components { get; set; }
        }

        public object SaveObject()
        {
            Log.Info($"Entity {name} is saving...");

            if (IsDynamic && !IsNetworkObjectSpawned)
            {
                Log.Warn($"Entity {name} is not valid, ignoring");
                return null;
            }

            SaveData data = new()
            {
                Metadata = Metadata,
                Spawned = IsNetworkObjectSpawned,
                Active = gameObject.activeSelf,
                PrefabId = GetPrefabId(),
                Components = new Dictionary<string, SaveToken>()
            };

            ISaveableComponent[] saveables = IsNetworkObjectSpawned ? GetComponents<ISaveableComponent>() : Array.Empty<ISaveableComponent>();
            foreach (ISaveableComponent saveable in saveables)
            {
                string saveableKey = saveable.ComponentKey;
                SaveToken saveableData;
                try
                {
                    object saved = saveable.SaveObject();
                    if (saved == null) continue;

                    saveableData = SaveToken.FromObject(saved);
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {name} failed to save component {saveableKey}: {e.Message}");
                    continue;
                }

                if (!data.Components.TryAdd(saveableKey, saveableData))
                {
                    Log.Warn($"Entity {name} found duplicate key {saveableKey}");
                }
            }

            Log.Info($"Entity {name} saved {data.Components.Count} components, spawned = {IsNetworkObjectSpawned}");
            return data;
        }

        public void LoadObject(SaveToken content)
        {
            LoadObject(content.ToObject<SaveData>());
        }

        private void LoadObject(SaveData data)
        {
            Log.Info($"Entity {name} is loading...");

            activeAfterThaw = data.Active;
            Metadata = data.Metadata;

            if (IsDynamic && !data.Spawned)
            {
                Log.Warn($"Entity {name} is not valid");
            }

            if (!data.Spawned)
            {
                if (data.Components.Count > 0)
                {
                    Log.Warn($"Entity {name} is not spawned, but has {data.Components.Count} serialized components, ignoring");
                }

                Log.Info($"Entity {name} is not spawned, despawning...");
                networkObject.Despawn(IsDynamic);
                return;
            }

            ISaveableComponent[] saveables = GetComponents<ISaveableComponent>();
            var knownKeys = new HashSet<string>();
            foreach (ISaveableComponent saveable in saveables)
            {
                string saveableKey = saveable.ComponentKey;
                knownKeys.Add(saveableKey);

                if (!data.Components.TryGetValue(saveableKey, out SaveToken componentData))
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

            foreach (string key in data.Components.Keys)
            {
                if (!knownKeys.Contains(key))
                {
                    Log.Warn($"Entity {name} found unknown saved property {key}");
                }
            }

            Log.Info($"Entity {name} loaded {knownKeys.Count} / {saveables.Length} components (provided {data.Components.Count})");
        }

        public static bool TrySpawnFromSave(FrozenWorld frozenWorld, string id, SaveToken content)
        {
            SaveData data = content.ToObject<SaveData>();
            if (!data.Spawned)
            {
                Log.Warn($"Entity id {id} is not valid, ignoring");
                return false;
            }

            string prefabId = data.PrefabId;
            if (string.IsNullOrEmpty(prefabId))
            {
                Log.Warn($"Entity id {id} has no prefab id, ignoring");
                return false;
            }

            SaveablePrefabAsset prefabAsset = Catalogs.GetCatalog<SaveablePrefabCatalog>().GetAssetById(prefabId);
            if (prefabAsset == null || prefabAsset.Prefab == null)
            {
                Log.Warn($"Failed to find prefab {prefabId} for entity id {id}, ignoring");
                return false;
            }

            GameObject spawnedObject = NetworkSpawner.InstantiateAndSpawn(prefabAsset.Prefab, instance =>
            {
                if (instance.TryGetComponent(out SaveableObject instanceSaveable)) instanceSaveable.id = id;
            });

            if (!spawnedObject.TryGetComponent(out SaveableObject saveableObject))
            {
                Log.Warn($"Prefab {prefabId} does not have saveable object");
                return false;
            }

            frozenWorld.FreezeSaveableObject(saveableObject);
            saveableObject.LoadObject(data);
            return true;
        }
    }
}
