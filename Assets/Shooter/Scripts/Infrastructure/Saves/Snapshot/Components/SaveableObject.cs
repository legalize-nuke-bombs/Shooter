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
        private bool active = true;

        public string Id => id;
        public bool Active => active;
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
            if (Static)
            {
                return null;
            }
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

            if (Dynamic && !Spawned)
            {
                Log.Warn($"Entity {name} is not valid, ignoring");
                return null;
            }

            SaveData data = new()
            {
                Metadata = Metadata,
                Spawned = Spawned,
                Active = gameObject.activeSelf,
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

                if (!data.Components.TryAdd(saveableKey, saveableData))
                {
                    Log.Warn($"Entity {name} found duplicate key {saveableKey}");
                }
            }

            Log.Info($"Entity {name} saved {data.Components.Count} components, spawned = {Spawned}");
            return data;
        }

        public void LoadObject(SaveToken content)
        {
            LoadObject(content.To<SaveData>());
        }

        private void LoadObject(SaveData data)
        {
            Log.Info($"Entity {name} is loading...");

            active = data.Active;
            Metadata = data.Metadata;

            if (Dynamic && !data.Spawned)
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
                networkObject.Despawn(Dynamic);
                return;
            }

            ISaveableComponent[] saveables = GetComponents<ISaveableComponent>();
            var known = new HashSet<string>();
            foreach (ISaveableComponent saveable in saveables)
            {
                string saveableKey = saveable.ComponentKey;
                known.Add(saveableKey);

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
                if (!known.Contains(key))
                {
                    Log.Warn($"Entity {name} found unknown saved property {key}");
                }
            }

            Log.Info($"Entity {name} loaded {known.Count} / {saveables.Length} components (provided {data.Components.Count})");
        }

        public static bool Spawn(FrozenWorld world, string id, SaveToken content)
        {
            SaveData data = content.To<SaveData>();
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
            SaveablePrefabAsset asset = Catalogs.Of<SaveablePrefabCatalog>().Of(prefabId);
            if (asset == null || asset.Prefab == null)
            {
                Log.Warn($"Failed to find prefab {prefabId} for entity id {id}, ignoring");
                return false;
            }

            GameObject body = NetworkSpawner.Spawn(asset.Prefab, fresh =>
            {
                if (fresh.TryGetComponent(out SaveableObject saved)) saved.id = id;
            });

            if (!body.TryGetComponent(out SaveableObject saveableObject))
            {
                Log.Warn($"Prefab {prefabId} does not have saveable object");
                return false;
            }

            world.Adopt(saveableObject);
            saveableObject.LoadObject(data);
            return true;
        }
    }
}
