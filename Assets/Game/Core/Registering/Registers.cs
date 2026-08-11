using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Core
{
    public class Registers : MonoBehaviour
    {
        private readonly Dictionary<Type, object> registers = new Dictionary<Type, object>();

        public Register<T> Of<T>() where T : Component
        {
            if (registers.TryGetValue(typeof(T), out object found)) return (Register<T>)found;

            var created = new Register<T>();
            registers[typeof(T)] = created;

            return created;
        }
    }
}
