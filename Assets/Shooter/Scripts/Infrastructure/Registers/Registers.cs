using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public static class Registers
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly Dictionary<Type, Register> RegistersByType = new();
        private static readonly Dictionary<Type, List<Register>> RegistersOfTypeAndSubtypes = new();

        public static void RegisterBehaviour<T>(T member) where T : MonoBehaviour, IShooterBehaviour
        {
            if (!RegistersByType.TryGetValue(member.GetType(), out Register register))
            {
                register = new Register();
                RegistersByType[member.GetType()] = register;
                RegistersOfTypeAndSubtypes.Clear();
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
            foreach (Register register in GetRegistersOfTypeAndSubtypes(typeof(T)))
            {
                foreach (MonoBehaviour member in register.GetMembers(inactivePolicy))
                {
                    yield return (T)member;
                }
            }
        }

        public static T Get<T>() where T : MonoBehaviour, IShooterBehaviour
        {
            return GetAll<T>(RegisterInactivePolicy.Include).Single();
        }

        private static List<Register> GetRegistersOfTypeAndSubtypes(Type type)
        {
            if (RegistersOfTypeAndSubtypes.TryGetValue(type, out List<Register> registers)) return registers;

            registers = new List<Register>();
            foreach (KeyValuePair<Type, Register> registerByType in RegistersByType)
            {
                if (type.IsAssignableFrom(registerByType.Key)) registers.Add(registerByType.Value);
            }

            RegistersOfTypeAndSubtypes[type] = registers;
            return registers;
        }
    }
}
