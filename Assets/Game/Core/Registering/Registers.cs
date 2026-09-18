using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Core
{
    public static class Registers
    {
        private static readonly Dictionary<Type, Register> Known = new();

        public static void Track<T>(T member) where T : Component, IRegistered
        {
            if (!Known.TryGetValue(member.GetType(), out Register register))
            {
                register = new Register();
                Known[member.GetType()] = register;
            }

            register.Add(member);
        }

        public static void Untrack<T>(T member) where T : Component, IRegistered
        {
            if (Known.TryGetValue(member.GetType(), out Register register)) register.Remove(member);
        }

        public static IEnumerable<T> Of<T>(Inactive gate) where T : Component, IRegistered
        {
            if (!Known.TryGetValue(typeof(T), out Register register)) yield break;

            foreach (Component member in register.All(gate)) yield return (T)member;
        }
    }
}
