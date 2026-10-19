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
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [Serializable]
        private struct Entry
        {
            [SerializeField] private ItemAsset asset;
            [SerializeField] private int amount;
            [SerializeField] private bool held;

            public ItemAsset Asset => asset;

            public int Amount => amount;

            public bool IsHeld => held;
        }

        [SerializeField] private Entry[] contents;

        private InventoryHolder inventoryHolder;
        private Inventory inventory;

        private void Awake()
        {
            inventoryHolder = GetComponent<InventoryHolder>();
            inventory = GetComponent<Inventory>();
        }

        protected override void OnNetworkPostSpawn()
        {
            if (!IsServer) return;

            foreach (Entry entry in contents)
            {
                if (entry.Asset == null) continue;

                AddEntryToInventory(entry);
            }

            Log.Info($"Entity {name} starts with {contents.Length} kinds of things in the bag");
        }

        private void AddEntryToInventory(Entry entry)
        {
            int amount = Math.Max(entry.Amount, 1);

            if (entry.Asset is StackableItemAsset stackableAsset)
            {
                inventory.AddStackable(stackableAsset, amount);
                return;
            }

            UniqueItemAsset uniqueAsset = (UniqueItemAsset)entry.Asset;

            for (int made = 0; made < amount; made++)
            {
                int slot = inventory.AddUnique(uniqueAsset.CreateItem());
                if (entry.IsHeld && made == 0) inventoryHolder.HoldSlot(slot);
            }
        }
    }
}
