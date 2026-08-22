using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Core
{
    [DefaultExecutionOrder(-110)]
    public class Registers : MonoBehaviour
    {
        private readonly Dictionary<Type, Register> registers = new();

        public static Registers Current { get; private set; }

        private void Awake()
        {
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public void Track<T>(T member) where T : Component, IRegistered
        {
            if (!registers.TryGetValue(member.GetType(), out Register register))
            {
                register = new Register();
                registers[member.GetType()] = register;
            }

            register.Add(member);
        }

        public void Untrack<T>(T member) where T : Component, IRegistered
        {
            if (registers.TryGetValue(member.GetType(), out Register register)) register.Remove(member);
        }

        public IEnumerable<T> Of<T>() where T : Component, IRegistered
        {
            if (!registers.TryGetValue(typeof(T), out Register register)) yield break;

            foreach (Component member in register.All) yield return (T)member;
        }
    }
}
