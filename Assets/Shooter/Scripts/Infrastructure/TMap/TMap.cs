using System;
using System.Collections.Generic;

namespace Shooter.Infrastructure
{
    public sealed class TMap
    {
        private static readonly Dictionary<Type, Type[]> KeysByBehaviourType = new();

        private Dictionary<Type, TArray> arraysByKey = new();

        public void Add(IShooterBehaviour behaviour)
        {
            foreach (Type key in GetKeys(behaviour.GetType()))
            {
                if (!arraysByKey.TryGetValue(key, out TArray array))
                {
                    array = new TArray();
                    arraysByKey[key] = array;
                }

                array.Add(behaviour);
            }
        }

        public void Remove(IShooterBehaviour behaviour)
        {
            foreach (Type key in GetKeys(behaviour.GetType()))
            {
                if (arraysByKey.TryGetValue(key, out TArray array)) array.Remove(behaviour);
            }
        }

        public TArray.View<T> GetAll<T>() where T : class
        {
            arraysByKey.TryGetValue(typeof(T), out TArray array);
            return new TArray.View<T>(array);
        }

        public void CollectGarbage()
        {
            int emptyArrays = 0;
            foreach (TArray array in arraysByKey.Values)
            {
                if (array.Count == 0) emptyArrays++;
                else array.CollectGarbage();
            }

            if (emptyArrays == 0) return;

            var keptArrays = new Dictionary<Type, TArray>(arraysByKey.Count - emptyArrays);
            foreach (KeyValuePair<Type, TArray> pair in arraysByKey)
            {
                if (pair.Value.Count > 0) keptArrays.Add(pair.Key, pair.Value);
            }

            arraysByKey = keptArrays;
        }

        // GetInterfaces builds a new array on every call, so the keys a behaviour goes under are found once per type
        private static Type[] GetKeys(Type behaviourType)
        {
            if (KeysByBehaviourType.TryGetValue(behaviourType, out Type[] keys)) return keys;

            var types = new List<Type>();
            for (Type type = behaviourType; typeof(IShooterBehaviour).IsAssignableFrom(type); type = type.BaseType)
            {
                types.Add(type);
            }

            types.AddRange(behaviourType.GetInterfaces());

            keys = types.ToArray();
            KeysByBehaviourType[behaviourType] = keys;
            return keys;
        }
    }
}
