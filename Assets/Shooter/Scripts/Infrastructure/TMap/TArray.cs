using System;

namespace Shooter.Infrastructure
{
    public sealed class TArray
    {
        private const int InitialCapacity = 4;

        private readonly Type elementType;

        private object[] elements;
        private int count;

        public TArray(Type elementType)
        {
            this.elementType = elementType;
            elements = (object[])Array.CreateInstance(elementType, InitialCapacity);
        }

        public ReadOnlySpan<T> AsSpan<T>() where T : class
        {
            return new ReadOnlySpan<T>((T[])elements, 0, count);
        }

        public void Add(IShooterBehaviour behaviour)
        {
            if (count == elements.Length)
            {
                var grownElements = (object[])Array.CreateInstance(elementType, elements.Length * 2);
                Array.Copy(elements, grownElements, count);
                elements = grownElements;
            }

            elements[count] = behaviour;
            count++;
        }

        public void Remove(IShooterBehaviour behaviour)
        {
            for (int i = 0; i < count; i++)
            {
                if (!ReferenceEquals(elements[i], behaviour)) continue;

                count--;
                elements[i] = elements[count];
                elements[count] = null;
                return;
            }
        }
    }
}
