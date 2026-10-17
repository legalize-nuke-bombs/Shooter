using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shooter.Infrastructure
{
    // Marks the object of the map that carries the state of its world. The clock, the conversations and the rest are
    // plain components next to it, asked for by type. Tracked in Awake, so the map has it before Netcode spawns anything
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class MapState : RegisteredBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<Type, Component> parts = new();

        protected override void Awake()
        {
            base.Awake();
            if (Registers.Of<MapState>(Inactive.Include).Count() > 1) Log.Error("A second map state has appeared");
        }

        // Null while there is no map: in the menu
        public static T Get<T>() where T : Component
        {
            foreach (MapState state in Registers.Of<MapState>(Inactive.Include))
            {
                return state.Part<T>();
            }
            return null;
        }

        private T Part<T>() where T : Component
        {
            if (parts.TryGetValue(typeof(T), out Component known)) return (T)known;

            T part = GetComponent<T>();
            if (part == null) Log.Error($"The map state carries no {typeof(T).Name}");

            parts[typeof(T)] = part;
            return part;
        }
    }
}
