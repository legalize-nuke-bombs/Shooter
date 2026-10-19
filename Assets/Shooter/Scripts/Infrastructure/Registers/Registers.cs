using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public static class Registers
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly Dictionary<Type, Register> RegistersByType = new();

        public static void Add<T>(T member) where T : MonoBehaviour, IShooterBehaviour
        {
            if (!RegistersByType.TryGetValue(member.GetType(), out Register register))
            {
                register = new Register();
                RegistersByType[member.GetType()] = register;
            }

            register.Add(member);
            Log.Debug($"{member.GetType().Name} {member.name} registered");
        }

        public static void Remove<T>(T member) where T : MonoBehaviour, IShooterBehaviour
        {
            if (RegistersByType.TryGetValue(member.GetType(), out Register register))
            {
                register.Remove(member);
            }

            Log.Debug($"{member.GetType().Name} {member.name} unregistered");
        }

        public static IEnumerable<T> Of<T>(RegisterInactivePolicy inactivePolicy) where T : MonoBehaviour, IShooterBehaviour
        {
            if (!RegistersByType.TryGetValue(typeof(T), out Register register)) yield break;

            foreach (MonoBehaviour member in register.All(inactivePolicy))
            {
                yield return (T)member;
            }
        }
    }
}
