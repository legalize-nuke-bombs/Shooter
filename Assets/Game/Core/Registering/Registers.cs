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

        private readonly Dictionary<Type, List<Component>> registers = new();

        public static Registers Current { get; private set; }

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

            if (!world.registers.TryGetValue(member.GetType(), out List<Component> register))
            {
                register = new List<Component>();
                world.registers[member.GetType()] = register;
            }

            register.Add(member);
        }

        public static void Untrack(Component member)
        {
            Registers world = Current;
            if (world == null) return;

            if (world.registers.TryGetValue(member.GetType(), out List<Component> register)) register.Remove(member);
        }

        public IEnumerable<T> Of<T>() where T : Component
        {
            if (!registers.TryGetValue(typeof(T), out List<Component> register)) yield break;

            foreach (Component member in register) yield return (T)member;
        }
    }
}
