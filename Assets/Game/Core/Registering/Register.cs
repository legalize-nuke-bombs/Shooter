using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Core
{
    public sealed class Register<T> where T : Component
    {
        private readonly Registers world;

        private readonly List<T> items = new();

        private readonly Dictionary<long, T> ids = new();

        private long version = -1;

        internal Register(Registers world)
        {
            this.world = world;
        }

        public int Count
        {
            get
            {
                Refresh();
                return items.Count;
            }
        }

        public IEnumerable<T> All
        {
            get
            {
                Refresh();
                return items;
            }
        }

        public T Of(long id)
        {
            Refresh();

            T found = ids.GetValueOrDefault(id, null);
            if (found != null && Identity(found) == id) return found;

            Reindex();
            return ids.GetValueOrDefault(id, null);
        }

        private void Refresh()
        {
            if (version == world.Version) return;

            items.Clear();
            foreach (Component member in world.Members)
                if (member is T typed)
                    items.Add(typed);

            Reindex();
            version = world.Version;
        }

        private void Reindex()
        {
            ids.Clear();
            foreach (T item in items)
                if (item is IIdentified identified)
                    ids[identified.Id] = item;
        }

        private static long Identity(T member)
        {
            return member is IIdentified identified ? identified.Id : long.MinValue;
        }
    }
}
