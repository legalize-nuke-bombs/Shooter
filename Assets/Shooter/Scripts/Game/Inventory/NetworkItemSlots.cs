using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public class NetworkItemSlots : NetworkVariableBase
    {
        private readonly List<UniqueItem> items = new();

        public IReadOnlyList<UniqueItem> All => items;

        public event Action Changed;

        public UniqueItem At(int slot)
        {
            return slot >= 0 && slot < items.Count ? items[slot] : null;
        }

        public bool Contains(UniqueItem item)
        {
            return items.Contains(item);
        }

        public int Put(UniqueItem item)
        {
            int slot = items.IndexOf(null);
            if (slot < 0) slot = items.Count;

            Put(slot, item);

            return slot;
        }

        public void Put(int slot, UniqueItem item)
        {
            while (items.Count <= slot)
            {
                items.Add(null);
            }

            items[slot] = item;
            item.Changed += OnChanged;

            OnChanged();
        }

        public UniqueItem Take(int slot)
        {
            UniqueItem item = At(slot);
            if (item == null) return null;

            items[slot] = null;
            item.Changed -= OnChanged;

            OnChanged();

            return item;
        }

        public void Clear()
        {
            foreach (UniqueItem item in items)
            {
                if (item == null) continue;

                item.Changed -= OnChanged;
            }

            items.Clear();

            OnChanged();
        }

        public override void WriteField(FastBufferWriter writer)
        {
            ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();

            writer.WriteValueSafe(items.Count);

            foreach (UniqueItem item in items)
            {
                writer.WriteValueSafe(catalog.Kind(item));
                if (item != null) writer.WriteNetworkSerializable(item);
            }
        }

        public override void ReadField(FastBufferReader reader)
        {
            ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();

            reader.ReadValueSafe(out int count);
            items.Clear();

            for (int slot = 0; slot < count; slot++)
            {
                reader.ReadValueSafe(out int kind);

                UniqueItem item = kind < 0 ? null : catalog.Create(kind);
                if (item != null) reader.ReadNetworkSerializableInPlace(ref item);

                items.Add(item);
            }

            Changed?.Invoke();
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            WriteField(writer);
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            ReadField(reader);
        }

        private void OnChanged()
        {
            SetDirty(true);
            Changed?.Invoke();
        }
    }
}
