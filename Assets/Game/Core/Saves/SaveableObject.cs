using System;
using System.Collections.Generic;
using Shooter.Game.Core.GameObject;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(GameObjectId))]
    public class SaveableObject : NetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private bool Spawned { get; set; } = true;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            GameObjectId id = GetComponent<GameObjectId>();
            if (!string.IsNullOrEmpty(id.Id) || NetworkObject.InScenePlaced) return;

            id.Assign(Guid.NewGuid().ToString());
            Log.Info($"Entity {name} got dynamic save id {id.Id}");
        }

        public override void OnNetworkDespawn()
        {
            Spawned = false;
        }

        public string ComponentKey => "SaveableObject";

        private struct SaveDto
        {
            public bool Spawned { get; set; }
            public string Metadata { get; set; }
        }

        public object SaveObject()
        {
            return new SaveDto
            {
                Spawned = Spawned,
                Metadata = name
            };
        }

        public void LoadObject(SaveToken content)
        {
            Spawned = content.To<SaveDto>().Spawned;
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

            Log.Info($"Entity {name} saved {components.Count} components, spawned = {Spawned}");
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

            Log.Info($"Entity {name} loaded {known.Count} / {saveables.Length} components (provided {components.Count}), spawned = {Spawned}");
            if (!Spawned) NetworkObject.Despawn(false);
        }
    }
}
