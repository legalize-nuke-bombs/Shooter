using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public class NetworkItemSlots : NetworkVariableBase
    {
        private readonly List<UniqueItem> items = new();

        public IReadOnlyList<UniqueItem> ItemsBySlot => items;

        public event Action Changed;

        public UniqueItem GetItemAt(int slot)
        {
            return slot >= 0 && slot < items.Count ? items[slot] : null;
        }

        public bool ContainsItem(UniqueItem item)
        {
            return items.Contains(item);
        }

        public int PutInFreeSlot(UniqueItem item)
        {
            int slot = items.IndexOf(null);
            if (slot < 0) slot = items.Count;

            PutInSlot(slot, item);

            return slot;
        }

        public void PutInSlot(int slot, UniqueItem item)
        {
            while (items.Count <= slot)
            {
                items.Add(null);
            }

            items[slot] = item;
            item.Changed += MarkDirtyAndRaiseChanged;

            MarkDirtyAndRaiseChanged();
        }

        public UniqueItem TakeFromSlot(int slot)
        {
            UniqueItem item = GetItemAt(slot);
            if (item == null) return null;

            items[slot] = null;
            item.Changed -= MarkDirtyAndRaiseChanged;

            MarkDirtyAndRaiseChanged();

            return item;
        }

        public void RemoveAllItems()
        {
            foreach (UniqueItem item in items)
            {
                if (item == null) continue;

                item.Changed -= MarkDirtyAndRaiseChanged;
            }

            items.Clear();

            MarkDirtyAndRaiseChanged();
        }

        public override void WriteField(FastBufferWriter writer)
        {
            ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();

            writer.WriteValueSafe(items.Count);

            foreach (UniqueItem item in items)
            {
                writer.WriteValueSafe(catalog.GetAssetIndexOf(item));
                if (item != null) writer.WriteNetworkSerializable(item);
            }
        }

        public override void ReadField(FastBufferReader reader)
        {
            ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();

            reader.ReadValueSafe(out int slotCount);
            items.Clear();

            for (int slot = 0; slot < slotCount; slot++)
            {
                reader.ReadValueSafe(out int assetIndex);

                UniqueItem item = assetIndex < 0 ? null : catalog.CreateUniqueItemAt(assetIndex);
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

        private void MarkDirtyAndRaiseChanged()
        {
            SetDirty(true);
            Changed?.Invoke();
        }
    }
}
