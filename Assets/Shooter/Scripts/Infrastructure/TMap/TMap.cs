using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public sealed class TMap
    {
        private static readonly Dictionary<Type, Type[]> KeysByComponentType = new();

        private Dictionary<Type, TArray> arraysByKey = new();

        public int KeyCount => arraysByKey.Count;

        public int UsedSlots
        {
            get
            {
                int usedSlots = 0;
                foreach (TArray array in arraysByKey.Values)
                {
                    usedSlots += array.Count;
                }

                return usedSlots;
            }
        }

        public int Capacity
        {
            get
            {
                int capacity = 0;
                foreach (TArray array in arraysByKey.Values)
                {
                    capacity += array.Capacity;
                }

                return capacity;
            }
        }

        public void Add(MonoBehaviour component)
        {
            foreach (Type key in GetKeys(component.GetType()))
            {
                if (!arraysByKey.TryGetValue(key, out TArray array))
                {
                    array = new TArray();
                    arraysByKey[key] = array;
                }

                array.Add(component);
            }
        }

        public void Remove(MonoBehaviour component)
        {
            foreach (Type key in GetKeys(component.GetType()))
            {
                if (arraysByKey.TryGetValue(key, out TArray array)) array.Remove(component);
            }
        }

        public int CountOf(Type key)
        {
            return arraysByKey.TryGetValue(key, out TArray array) ? array.Count : 0;
        }

        public TArray.View<T> GetAll<T>() where T : class
        {
            arraysByKey.TryGetValue(typeof(T), out TArray array);
            return new TArray.View<T>(array);
        }


        public void FindLargestClasses(Type[] classes, int[] counts)
        {
            Array.Clear(classes, 0, classes.Length);
            Array.Clear(counts, 0, counts.Length);

            foreach (KeyValuePair<Type, TArray> pair in arraysByKey)
            {
                if (pair.Key.IsInterface || pair.Key.IsAbstract) continue;

                int count = pair.Value.Count;
                for (int place = 0; place < counts.Length; place++)
                {
                    if (count <= counts[place]) continue;

                    Array.Copy(counts, place, counts, place + 1, counts.Length - place - 1);
                    Array.Copy(classes, place, classes, place + 1, classes.Length - place - 1);
                    counts[place] = count;
                    classes[place] = pair.Key;
                    break;
                }
            }
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

        // GetInterfaces builds a new array on every call, so the keys a component goes under are found once per type
        private static Type[] GetKeys(Type componentType)
        {
            if (KeysByComponentType.TryGetValue(componentType, out Type[] keys)) return keys;

            var types = new List<Type>();
            for (Type type = componentType; type != typeof(MonoBehaviour); type = type.BaseType)
            {
                types.Add(type);
            }

            types.AddRange(componentType.GetInterfaces());

            keys = types.ToArray();
            KeysByComponentType[componentType] = keys;
            return keys;
        }
    }
}
