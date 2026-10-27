using System;
using System.Runtime.CompilerServices;

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

        public void Add(IShooterBehaviour behaviour)
        {
            int hash = RuntimeHelpers.GetHashCode(behaviour);
            if (FindSlot(behaviour, hash) != None) return;

            int position = TakePosition();
            entries[position] = new Entry { Behaviour = behaviour, Hash = hash, NextFree = None };
            Place(position);
            count++;
        }

        public void Remove(IShooterBehaviour behaviour)
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

        private int FindSlot(IShooterBehaviour behaviour, int hash)
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
                if (entries[position].Behaviour == null) continue;

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
                if (entries[position].Behaviour != null) Place(position);
            }
        }

        private struct Entry
        {
            public IShooterBehaviour Behaviour;
            public int Hash;
            public int NextFree;
        }

        public readonly struct View<T> where T : class
        {
            private readonly TArray array;

            public View(TArray array)
            {
                this.array = array;
            }

            public int Count => array == null ? 0 : array.count;

            public Enumerator GetEnumerator()
            {
                return new Enumerator(array);
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
                private int position;
                private T current;

                public Enumerator(TArray array)
                {
                    this.array = array;
                    position = 0;
                    current = null;
                }

                public T Current => current;

                public bool MoveNext()
                {
                    while (array != null && position < array.used)
                    {
                        IShooterBehaviour behaviour = array.entries[position].Behaviour;
                        position++;
                        if (behaviour == null) continue;

                        current = (T)behaviour;
                        return true;
                    }

                    return false;
                }
            }
        }
    }
}
