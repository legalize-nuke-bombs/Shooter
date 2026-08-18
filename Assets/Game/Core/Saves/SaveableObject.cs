using System;
using System.Collections.Generic;
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

        public string ComponentKey()
        {
            return "SaveableObject";
        }
        struct SaveDto
        {
            public bool Spawned { get; set; }
        }
        public object SaveComponent()
        {
            return new SaveDto()
            {
                Spawned = Spawned
            };
        }
        public void LoadComponent(object content)
        {
            var save = (SaveDto)content;
            Spawned = save.Spawned;
        }




        private ISaveableComponent[] saveables;

        private void Awake()
        {
            saveables = GetComponents<ISaveableComponent>();
        }

        private object Save()
        {
            var components = new Dictionary<string, object>();
            foreach (ISaveableComponent saveable in saveables)
            {
                if (!components.TryAdd(saveable.ComponentKey(), saveable.SaveComponent()))
                {
                    throw new ArgumentException($"Duplicate component key {saveable.ComponentKey()}");
                }
            }
            return components;
        }
        private void Load(object content)
        {
            var components = (Dictionary<string, object>)content;
            foreach (ISaveableComponent saveable in saveables)
            {
                if (!components.TryGetValue(saveable.ComponentKey(), out object componentData))
                {
                    Log.Warn($"Failed to find component {saveable.ComponentKey()} data!");
                    continue;
                }
                saveable.LoadComponent(componentData);
            }
        }
    }
}
