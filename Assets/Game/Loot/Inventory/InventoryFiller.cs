using System;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(InventoryHolder))]
    public class InventoryFiller : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [Serializable]
        private struct Entry
        {
            [SerializeField] private ItemSpec spec;
            [SerializeField] private int amount;
            [SerializeField] private bool held;

            public ItemSpec Spec => spec;

            public int Amount => amount;

            public bool Held => held;
        }

        [SerializeField] private Entry[] contents;

        private InventoryHolder holder;
        private Inventory inventory;

        private void Awake()
        {
            holder = GetComponent<InventoryHolder>();
            inventory = GetComponent<Inventory>();
        }

        protected override void OnNetworkPostSpawn()
        {
            if (!IsServer) return;

            foreach (Entry entry in contents)
            {
                if (entry.Spec == null) continue;

                Fill(entry);
            }

            Log.Info($"Entity {name} starts with {contents.Length} kinds of things in the bag");
        }

        private void Fill(Entry entry)
        {
            int amount = Math.Max(entry.Amount, 1);

            if (entry.Spec is StackableItemSpec stackable)
            {
                inventory.Add(stackable, amount);
                return;
            }

            var unique = (UniqueItemSpec)entry.Spec;

            for (int made = 0; made < amount; made++)
            {
                int slot = inventory.Add(unique.Create());
                if (entry.Held && made == 0) holder.Hold(slot);
            }
        }
    }
}
