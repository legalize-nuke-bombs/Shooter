using System;
using System.Collections.Generic;

namespace Shooter.Infrastructure
{
    public sealed class TArray
    {
        private const int InitialCapacity = 4;

        private readonly Dictionary<IShooterBehaviour, int> positions = new();
        private readonly Stack<int> freePositions = new();

        private IShooterBehaviour[] elements = new IShooterBehaviour[InitialCapacity];
        private int used;

        public int Count => positions.Count;

        internal int Used => used;

        internal IShooterBehaviour this[int position] => elements[position];

        public void Add(IShooterBehaviour behaviour)
        {
            if (positions.ContainsKey(behaviour)) return;

            int position = freePositions.Count > 0 ? freePositions.Pop() : TakeNewPosition();
            elements[position] = behaviour;
            positions.Add(behaviour, position);
        }

        public void Remove(IShooterBehaviour behaviour)
        {
            if (!positions.Remove(behaviour, out int position)) return;

            elements[position] = null;
            if (positions.Count == 0)
            {
                used = 0;
                freePositions.Clear();
                return;
            }

            freePositions.Push(position);
        }

        private int TakeNewPosition()
        {
            if (used == elements.Length) Array.Resize(ref elements, elements.Length * 2);
            return used++;
        }
    }
}
