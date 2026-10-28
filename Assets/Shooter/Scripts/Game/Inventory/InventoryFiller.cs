using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    public class InventoryFiller : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [Serializable]
        private struct Entry
        {
            [SerializeField] private ItemAsset asset;
            [SerializeField] private int amount;

            public ItemAsset Asset => asset;

            public int Amount => amount;
        }

        [SerializeField] private Entry[] contents;

        private Inventory inventory;

        private void Awake()
        {
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
                inventory.AddUnique(uniqueAsset.CreateItem());
            }
        }
    }
}
