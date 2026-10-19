using System;
using System.Collections.Generic;

namespace Shooter.Infrastructure
{
    public static class Registers
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly Dictionary<Type, Register> Known = new();

        public static void Add(ShooterBehaviour member)
        {
            if (!Known.TryGetValue(member.GetType(), out Register register))
            {
                register = new Register();
                Known[member.GetType()] = register;
            }

            register.Add(member);
            Log.Debug($"{member.GetType().Name} {member.name} registered");
        }

        public static void Remove(ShooterBehaviour member)
        {
            if (Known.TryGetValue(member.GetType(), out Register register))
            {
                register.Remove(member);
            }

            Log.Debug($"{member.GetType().Name} {member.name} unregistered");
        }

        public static IEnumerable<T> Of<T>(RegisterInactivePolicy gate) where T : ShooterBehaviour
        {
            if (!Known.TryGetValue(typeof(T), out Register register)) yield break;

            foreach (ShooterBehaviour member in register.All(gate))
            {
                yield return (T)member;
            }
        }
    }
}
