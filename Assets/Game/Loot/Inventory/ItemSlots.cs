using System;
using System.Collections.Generic;
using Shooter.Game.Packing;
using Unity.Netcode;

namespace Shooter.Game.Loot
{
    public class ItemSlots : INetworkSerializable, IEquatable<ItemSlots>
    {
        private readonly List<UniqueItem> items = new List<UniqueItem>();

        private int revision;

        public IReadOnlyList<UniqueItem> All => items;

        public int Count => items.Count;

        public bool Dirty
        {
            get
            {
                foreach (UniqueItem item in items)
                {
                    if (item != null && item.Dirty) return true;
                }

                return false;
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
            {
                if (item != null) item.Clean();
            }

            revision++;
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

            for (int slot = 0; slot < count; slot++)
            {
                bool filled = serializer.IsWriter && items[slot] != null;
                serializer.SerializeValue(ref filled);

                if (!filled) continue;

                Packed<UniqueItem> packed = serializer.IsWriter
                    ? new Packed<UniqueItem>(items[slot])
                    : default;

                packed.NetworkSerialize(serializer);

                if (serializer.IsReader) items[slot] = packed.Value;
            }
        }
    }
}
