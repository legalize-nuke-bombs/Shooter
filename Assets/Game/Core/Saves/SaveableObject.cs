using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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




        public Dictionary<string, object> Save()
        {
            Log.Info($"Entity {name} is saving...");

            ISaveableComponent[] saveables = GetComponents<ISaveableComponent>();
            var components = new Dictionary<string, object>();
            foreach (ISaveableComponent saveable in saveables)
            {
                string saveableKey = saveable.ComponentKey;
                object saveableData = null;
                try
                {
                    saveableData = saveable.SaveObject();
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {name} failed to save component {saveableKey}: {e.Message}");
                }
                if (saveableData == null)
                {
                    continue;
                }

                if (!components.TryAdd(saveableKey, saveableData))
                {
                    Log.Warn($"Entity {name} found duplicate key {saveableKey}");
                    continue;
                }
            }

            Log.Info($"Entity {name} saved {components.Count} components, spawned = {Spawned}");
            return components;
        }

        public void Load(JToken content)
        {
            Log.Info($"Entity {name} is loading...");

            ISaveableComponent[] saveables = GetComponents<ISaveableComponent>();
            var components = (JObject)content;
            var known = new HashSet<string>();
            foreach (ISaveableComponent saveable in saveables)
            {
                string saveableKey = saveable.ComponentKey;
                known.Add(saveableKey);

                if (!components.TryGetValue(saveableKey, out JToken componentData))
                {
                    Log.Warn($"Entity {name} failed to find saved data for {saveableKey}");
                    continue;
                }

                try
                {
                    saveable.LoadObject(new SaveToken(componentData));
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {name} failed to load component {saveableKey}: {e.Message}");
                    continue;
                }
            }

            foreach (JProperty property in components.Properties())
            {
                if (!known.Contains(property.Name))
                {
                    Log.Warn($"Entity {name} found unknown saved property {property.Name}");
                }
            }

            Log.Info($"Entity {name} loaded {known.Count} / {saveables.Length} components (provided {components.Count}), spawned = {Spawned}");
            if (!Spawned)
            {
                GetComponent<NetworkObject>().Despawn(false);
            }
        }
    }
}
