using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(InventoryHolder))]
    public class InventoryHolderFiller : ShooterNetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private UniqueItemAsset heldAsset;

        private Inventory inventory;
        private InventoryHolder inventoryHolder;

        protected override void ShooterAwake()
        {
            inventory = GetComponent<Inventory>();
            inventoryHolder = GetComponent<InventoryHolder>();
        }

        protected override void OnNetworkPostSpawn()
        {
            if (!IsServer) return;

            inventoryHolder.HoldSlot(inventory.AddUnique(heldAsset.CreateItem()));
            Log.Info($"Entity {name} starts with {heldAsset.Id} in its hands");
        }
    }
}
