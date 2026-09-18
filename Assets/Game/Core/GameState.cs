using System;
using System.Collections.Generic;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core
{
    // The state of the world. The session spawns it before the map loads, so no scene has to carry it.
    // It knows nothing of its parts: they are plain components on its prefab, asked for by type.
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class GameState : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static GameState current;

        private readonly Dictionary<Type, Component> parts = new();

        private void Awake()
        {
            if (current != null)
            {
                Log.Error("A second game state has appeared");
            }
            current = this;
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
        }

        // Null while there is no world: in the menu, and on a client until the state arrives over the network
        public static T Get<T>() where T : Component
        {
            if (current == null) return null;

            if (current.parts.TryGetValue(typeof(T), out Component known)) return (T)known;

            T part = current.GetComponent<T>();
            if (part == null) Log.Error($"The game state prefab carries no {typeof(T).Name}");

            current.parts[typeof(T)] = part;
            return part;
        }
    }
}
