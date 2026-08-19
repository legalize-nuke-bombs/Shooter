using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public class SaveableObject : MonoBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();




        [SerializeField] private string id;
        public string Id => id;

        [SerializeField] private string prefabKey;




        public bool Spawned { get; set; } = true;

        public string ComponentKey => "SaveableObject";

        private struct SaveDto
        {
            public bool Spawned { get; set; }

            public string PrefabKey { get; set; }
        }

        public object SaveComponent()
        {
            return new SaveDto
            {
                Spawned = Spawned,
                PrefabKey = prefabKey
            };
        }

        public void LoadComponent(JToken content)
        {
            Spawned = content.ToObject<SaveDto>().Spawned;
        }




        private ISaveableComponent[] saveables;

        private long registered;

        private void Awake()
        {
            saveables = GetComponents<ISaveableComponent>();
            registered = Registers.Current.Of<SaveableObject>().Add(this);
        }

        private void OnDestroy()
        {
            Registers world = Registers.Current;
            if (world != null) world.Of<SaveableObject>().Remove(registered);
        }

        public Dictionary<string, object> Save()
        {
            var components = new Dictionary<string, object>();
            foreach (ISaveableComponent saveable in saveables)
            {
                string saveableKey = saveable.ComponentKey;
                object saveableData = null;
                try
                {
                    saveableData = saveable.SaveComponent();
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

            return components;
        }

        public void Load(JToken content)
        {
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
                    saveable.LoadComponent(componentData);
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
        }
    }
}
