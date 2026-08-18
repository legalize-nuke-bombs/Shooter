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




        public bool Spawned { get; set; } = true;

        public string ComponentKey => "spawned";

        private struct SaveDto
        {
            public bool Spawned { get; set; }
        }

        public object SaveComponent()
        {
            return new SaveDto
            {
                Spawned = Spawned
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
                if (!components.TryAdd(saveable.ComponentKey, saveable.SaveComponent()))
                {
                    throw new ArgumentException($"Duplicate component key {saveable.ComponentKey} on {name}");
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
                known.Add(saveable.ComponentKey);
                if (!components.TryGetValue(saveable.ComponentKey, out JToken componentData))
                {
                    Log.Warn($"No saved data for component {saveable.ComponentKey} of {name}, it keeps defaults");
                    continue;
                }

                saveable.LoadComponent(componentData);
            }

            foreach (JProperty property in components.Properties())
            {
                if (!known.Contains(property.Name)) Log.Warn($"Orphan component data {property.Name} of {name}, ignored");
            }
        }
    }
}
