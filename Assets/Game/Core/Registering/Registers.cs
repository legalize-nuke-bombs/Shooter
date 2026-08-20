using System;
using System.Collections.Generic;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core
{
    [DefaultExecutionOrder(-110)]
    public class Registers : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<Type, object> registers = new();

        private readonly HashSet<Component> members = new();

        private long version;

        public static Registers Current { get; private set; }

        internal long Version => version;

        internal IReadOnlyCollection<Component> Members => members;

        private void Awake()
        {
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public static void Track(Component member)
        {
            Registers world = Current;
            if (world == null)
            {
                Log.Warn($"Entity {member.name} appeared before the world has registers and is lost to them");
                return;
            }

            if (world.members.Add(member)) world.version++;
        }

        public static void Untrack(Component member)
        {
            Registers world = Current;
            if (world == null) return;

            if (world.members.Remove(member)) world.version++;
        }

        public Register<T> Of<T>() where T : Component
        {
            if (registers.TryGetValue(typeof(T), out object found)) return (Register<T>)found;

            var created = new Register<T>(this);
            registers[typeof(T)] = created;

            return created;
        }
    }
}
