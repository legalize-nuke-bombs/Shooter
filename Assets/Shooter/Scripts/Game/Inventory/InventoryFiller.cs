using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(InventoryHolder))]
    public class InventoryFiller : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [Serializable]
        private struct Entry
        {
            [SerializeField] private ItemAsset asset;
            [SerializeField] private int amount;
            [SerializeField] private bool held;

            public ItemAsset Asset => asset;

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
                if (entry.Asset == null) continue;

                Fill(entry);
            }

            Log.Info($"Entity {name} starts with {contents.Length} kinds of things in the bag");
        }

        private void Fill(Entry entry)
        {
            int amount = Math.Max(entry.Amount, 1);

            if (entry.Asset is StackableItemAsset stackable)
            {
                inventory.Add(stackable, amount);
                return;
            }

            var unique = (UniqueItemAsset)entry.Asset;

            for (int made = 0; made < amount; made++)
            {
                int slot = inventory.Add(unique.Create());
                if (entry.Held && made == 0) holder.Hold(slot);
            }
        }
    }
}
