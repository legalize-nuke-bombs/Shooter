using System;
using System.Collections.Generic;

namespace Shooter.Infrastructure
{
    public static class Registers
    {
        private static readonly Dictionary<Type, Register> Known = new();

        public static void Add(RegisteredBehaviour member)
        {
            if (!Known.TryGetValue(member.GetType(), out Register register))
            {
                register = new Register();
                Known[member.GetType()] = register;
            }

            register.Add(member);
        }

        public static void Remove(RegisteredBehaviour member)
        {
            if (Known.TryGetValue(member.GetType(), out Register register))
            {
                register.Remove(member);
            }
        }

        public static IEnumerable<T> Of<T>(Inactive gate) where T : RegisteredBehaviour
        {
            if (!Known.TryGetValue(typeof(T), out Register register)) yield break;

            foreach (RegisteredBehaviour member in register.All(gate))
            {
                yield return (T)member;
            }
        }
    }
}
