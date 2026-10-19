using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public static class Registers
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();
        private static readonly Dictionary<Type, Register> RegistersByType = new();

        public static void RegisterBehaviour<T>(T member) where T : MonoBehaviour, IShooterBehaviour
        {
            if (!RegistersByType.TryGetValue(member.GetType(), out Register register))
            {
                register = new Register();
                RegistersByType[member.GetType()] = register;
            }

            register.AddMember(member);
            Log.Debug($"{member.GetType().Name} {member.name} registered");
        }

        public static void UnregisterBehaviour<T>(T member) where T : MonoBehaviour, IShooterBehaviour
        {
            if (RegistersByType.TryGetValue(member.GetType(), out Register register))
            {
                register.RemoveMember(member);
            }

            Log.Debug($"{member.GetType().Name} {member.name} unregistered");
        }

        public static IEnumerable<T> GetAll<T>(RegisterInactivePolicy inactivePolicy) where T : MonoBehaviour, IShooterBehaviour
        {
            if (!RegistersByType.TryGetValue(typeof(T), out Register register)) yield break;

            foreach (MonoBehaviour member in register.GetMembers(inactivePolicy))
            {
                yield return (T)member;
            }
        }
    }
}
