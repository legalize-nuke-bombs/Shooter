using System;
using System.Collections.Generic;
using Shooter.Game.Core;
using Unity.Netcode;

namespace Shooter.Game.Loot
{
    public class ItemSlots : INetworkSerializable, IEquatable<ItemSlots>
    {
        private readonly List<UniqueItem> items = new();

        private int revision;

        public IReadOnlyList<UniqueItem> All => items;

        public int Count => items.Count;

        public bool Dirty
        {
            get
            {
                foreach (UniqueItem item in items)
                    if (item != null && item.Dirty)
                        return true;

                return false;
            }
        }

        public bool Equals(ItemSlots other)
        {
            return other != null && revision == other.revision;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref revision);

            byte count = serializer.IsWriter ? (byte)items.Count : (byte)0;
            serializer.SerializeValue(ref count);

            if (serializer.IsReader)
            {
                items.Clear();
                for (int slot = 0; slot < count; slot++) items.Add(null);
            }

            ItemCatalog catalog = Catalogs.Of<ItemCatalog>();

            for (int slot = 0; slot < count; slot++)
            {
                int kind = serializer.IsWriter && catalog != null ? catalog.Kind(items[slot]) : -1;

                bool filled = serializer.IsWriter && kind >= 0;
                serializer.SerializeValue(ref filled);

                if (!filled) continue;

                serializer.SerializeValue(ref kind);

                if (serializer.IsReader && catalog != null) items[slot] = catalog.Create(kind);
                if (items[slot] == null) continue;

                items[slot].NetworkSerialize(serializer);
            }
        }

        public UniqueItem At(int slot)
        {
            return slot >= 0 && slot < items.Count ? items[slot] : null;
        }

        public bool Contains(UniqueItem item)
        {
            return item != null && items.Contains(item);
        }

        public int Put(UniqueItem item)
        {
            if (item == null) return -1;

            item.Clean();
            revision++;

            int slot = items.IndexOf(null);

            if (slot < 0)
            {
                slot = items.Count;
                items.Add(item);

                return slot;
            }

            items[slot] = item;

            return slot;
        }

        public UniqueItem Take(int slot)
        {
            UniqueItem item = At(slot);
            if (item == null) return null;

            items[slot] = null;
            revision++;

            return item;
        }

        public void Clear()
        {
            items.Clear();
            revision++;
        }

        public void Settle()
        {
            foreach (UniqueItem item in items)
                if (item != null)
                    item.Clean();

            revision++;
        }
    }
}
