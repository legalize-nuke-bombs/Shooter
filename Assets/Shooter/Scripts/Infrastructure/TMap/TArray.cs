using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public sealed class TArray
    {
        private const int MinCapacity = 4;
        private const int None = -1;

        private Entry[] entries;
        private int[] index;
        private int used;
        private int count;
        private int freeHead = None;

        public TArray()
        {
            entries = new Entry[MinCapacity];
            RebuildIndex();
        }

        public int Count => count;

        public int Capacity => entries.Length;

        public void Add(MonoBehaviour behaviour)
        {
            int hash = RuntimeHelpers.GetHashCode(behaviour);
            if (FindSlot(behaviour, hash) != None) return;

            int position = TakePosition();
            entries[position] = new Entry { Behaviour = behaviour, Hash = hash, NextFree = None };
            Place(position);
            count++;
        }

        public void Remove(MonoBehaviour behaviour)
        {
            int hole = FindSlot(behaviour, RuntimeHelpers.GetHashCode(behaviour));
            if (hole == None) return;

            int position = index[hole];
            entries[position] = new Entry { NextFree = freeHead };
            freeHead = position;
            count--;

            int mask = index.Length - 1;
            index[hole] = None;
            for (int i = (hole + 1) & mask; index[i] != None; i = (i + 1) & mask)
            {
                int home = entries[index[i]].Hash & mask;
                if (((i - home) & mask) < ((i - hole) & mask)) continue;

                index[hole] = index[i];
                index[i] = None;
                hole = i;
            }

            if (count == 0)
            {
                used = 0;
                freeHead = None;
            }
        }

        public void CollectGarbage()
        {
            if (entries.Length > MinCapacity && count <= entries.Length / 4) ShrinkToFit();
        }

        private int FindSlot(MonoBehaviour behaviour, int hash)
        {
            int mask = index.Length - 1;
            for (int i = hash & mask; index[i] != None; i = (i + 1) & mask)
            {
                if (ReferenceEquals(entries[index[i]].Behaviour, behaviour)) return i;
            }

            return None;
        }

        private void Place(int position)
        {
            int mask = index.Length - 1;
            int i = entries[position].Hash & mask;
            while (index[i] != None)
            {
                i = (i + 1) & mask;
            }

            index[i] = position;
        }

        private int TakePosition()
        {
            if (freeHead != None)
            {
                int position = freeHead;
                freeHead = entries[position].NextFree;
                return position;
            }

            if (used == entries.Length) Grow();
            return used++;
        }

        private void Grow()
        {
            Array.Resize(ref entries, entries.Length * 2);
            RebuildIndex();
        }

        private void ShrinkToFit()
        {
            int capacity = MinCapacity;
            while (capacity < 2 * count)
            {
                capacity *= 2;
            }

            var packed = new Entry[capacity];
            int packedUsed = 0;
            for (int position = 0; position < used; position++)
            {
                if (entries[position].Behaviour is null) continue;

                packed[packedUsed] = entries[position];
                packedUsed++;
            }

            entries = packed;
            used = packedUsed;
            freeHead = None;
            RebuildIndex();
        }

        private void RebuildIndex()
        {
            index = new int[entries.Length * 2];
            Array.Fill(index, None);
            for (int position = 0; position < used; position++)
            {
                if (entries[position].Behaviour is not null) Place(position);
            }
        }

        private struct Entry
        {
            public MonoBehaviour Behaviour;
            public int Hash;
            public int NextFree;
        }

        public readonly struct View<T> where T : class
        {
            private readonly TArray array;
            private readonly RegisterInactivePolicy inactivePolicy;

            public View(TArray array, RegisterInactivePolicy inactivePolicy)
            {
                this.array = array;
                this.inactivePolicy = inactivePolicy;
            }

            public int Count
            {
                get
                {
                    if (array == null) return 0;
                    if (inactivePolicy == RegisterInactivePolicy.Include) return array.count;

                    int count = 0;
                    foreach (T element in this)
                    {
                        count++;
                    }

                    return count;
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(array, inactivePolicy);
            }

            public bool TryGetFirst(out T first)
            {
                foreach (T element in this)
                {
                    first = element;
                    return true;
                }

                first = null;
                return false;
            }

            public ref struct Enumerator
            {
                private readonly TArray array;
                private readonly RegisterInactivePolicy inactivePolicy;
                private int position;
                private T current;

                public Enumerator(TArray array, RegisterInactivePolicy inactivePolicy)
                {
                    this.array = array;
                    this.inactivePolicy = inactivePolicy;
                    position = 0;
                    current = null;
                }

                public T Current => current;

                public bool MoveNext()
                {
                    while (array != null && position < array.used)
                    {
                        MonoBehaviour behaviour = array.entries[position].Behaviour;
                        position++;
                        if (behaviour is null) continue;
                        if (inactivePolicy == RegisterInactivePolicy.Exclude && !behaviour.isActiveAndEnabled) continue;

                        current = (T)(object)behaviour;
                        return true;
                    }

                    return false;
                }
            }
        }
    }
}
