namespace Shooter.Infrastructure
{
    public readonly struct TArrayView<T> where T : class
    {
        private readonly TArray array;

        internal TArrayView(TArray array)
        {
            this.array = array;
        }

        public int Count => array == null ? 0 : array.Count;

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

        public struct Enumerator
        {
            private readonly TArray array;
            private int position;
            private T current;

            internal Enumerator(TArray array)
            {
                this.array = array;
                position = 0;
                current = null;
            }

            public T Current => current;

            public bool MoveNext()
            {
                while (array != null && position < array.Used)
                {
                    IShooterBehaviour element = array[position];
                    position++;
                    if (element == null) continue;

                    current = (T)element;
                    return true;
                }

                return false;
            }
        }
    }
}
