using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Core
{
    [DefaultExecutionOrder(-110)]
    public class Registers : MonoBehaviour
    {
        public static Registers Current { get; private set; }

        private readonly Dictionary<Type, object> registers = new Dictionary<Type, object>();

        private void Awake()
        {
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public Register<T> Of<T>() where T : Component
        {
            if (registers.TryGetValue(typeof(T), out object found)) return (Register<T>)found;

            var created = new Register<T>();
            registers[typeof(T)] = created;

            return created;
        }
    }
}
