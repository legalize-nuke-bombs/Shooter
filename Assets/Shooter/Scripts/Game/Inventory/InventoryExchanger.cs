using System;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Character))]
    public class InventoryExchanger : ShooterNetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float radius = 10f;

        private Character character;
        private Inventory inventory;

        public float Radius => radius;

        public event Action<Character, ItemAsset, int> ReceivedFromCharacter;

        protected override void ShooterAwake()
        {
            character = GetComponent<Character>();
            inventory = GetComponent<Inventory>();
        }

        public bool TryGiveStackable(Inventory receiverInventory, StackableItemAsset asset, int amount)
        {
            if (amount <= 0 || !CanGiveTo(receiverInventory)) return false;

            if (inventory.RemoveStackable(asset, amount, InventoryShortagePolicy.Rollback) != amount)
            {
                Log.Info($"Entity {name} can not give {asset.Id} x {amount} to {receiverInventory.name}: not enough in the bag");
                return false;
            }

            InventoryExchanger receiverExchanger = receiverInventory.GetComponent<InventoryExchanger>();
            if (receiverExchanger)
            {
                receiverExchanger.ReceiveStackable(character, asset, amount);
            }
            else
            {
                receiverInventory.AddStackable(asset, amount);
            }
            Log.Info($"Entity {name} gave {asset.Id} x {amount} to {receiverInventory.name}");
            return true;
        }

        public bool TryGiveUnique(Inventory receiverInventory, int slot)
        {
            if (!CanGiveTo(receiverInventory)) return false;

            UniqueItem item = inventory.RemoveUniqueAt(slot);
            if (item == null)
            {
                Log.Info($"Entity {name} can not give slot {slot} to {receiverInventory.name}: the slot is empty");
                return false;
            }

            InventoryExchanger receiverExchanger = receiverInventory.GetComponent<InventoryExchanger>();
            if (receiverExchanger)
            {
                receiverExchanger.ReceiveUnique(character, item);
            }
            else
            {
                receiverInventory.AddUnique(item);
            }
            Log.Info($"Entity {name} gave {item.AssetId} from slot {slot} to {receiverInventory.name}");
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void GiveStackableRpc(long receiverId, FixedString32Bytes stackableId, int amount)
        {
            StackableItemAsset asset = Catalogs.GetCatalog<ItemCatalog>().GetAssetById(stackableId) as StackableItemAsset;
            if (asset == null)
            {
                Log.Info($"Entity {name} failed to rpc give {stackableId} x {amount} to {receiverId} : asset not found");
            }

            IncrementalId receiver = IncrementalId.FindByValue(receiverId, RegisterInactivePolicy.Exclude);
            if (receiver == null)
            {
                Log.Info($"Entity {name} failed to rpc give {stackableId} x {amount} to {receiverId} : target does not exist");
                return;
            }
            Inventory receivedInventory = receiver.GetComponent<Inventory>();
            if (receivedInventory == null)
            {
                Log.Info($"Entity {name} failed to rpc give {stackableId} x {amount} to {receiverId}: target does not contain inventory");
                return;
            }

            if (!TryGiveStackable(receivedInventory, asset, amount))
            {
                Log.Info($"Entity {name} failed to rpc give {stackableId} x {amount} to {receiverId}");
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void GiveUniqueRpc(long receiverId, int slot)
        {
            IncrementalId receiver = IncrementalId.FindByValue(receiverId, RegisterInactivePolicy.Exclude);
            if (receiver == null)
            {
                Log.Info($"Entity {name} failed to rpc give slot {slot} to {receiverId} : target does not exist");
                return;
            }
            Inventory receiverInventory = receiver.GetComponent<Inventory>();
            if (receiverInventory == null)
            {
                Log.Info($"Entity {name} failed to rpc give slot {slot} to {receiverId}: target does not contain inventory");
                return;
            }

            if (!TryGiveUnique(receiverInventory, slot))
            {
                Log.Info($"Entity {name} failed to rpc give slot {slot} to {receiverId}");
            }
        }

        private void ReceiveStackable(Character giver, StackableItemAsset asset, int amount)
        {
            inventory.AddStackable(asset, amount);
            ReceivedFromCharacter?.Invoke(giver, asset, amount);
        }

        private void ReceiveUnique(Character giver, UniqueItem item)
        {
            inventory.AddUnique(item);
            ReceivedFromCharacter?.Invoke(giver, Catalogs.GetCatalog<ItemCatalog>().GetAssetById(item.AssetId), 1);
        }

        private bool CanGiveTo(Inventory receiverInventory)
        {
            if (receiverInventory == inventory || !receiverInventory.gameObject.activeInHierarchy)
            {
                Log.Info($"Entity {name} has nobody to give things to");
                return false;
            }

            if (Vector3.Distance(receiverInventory.transform.position, transform.position) > radius)
            {
                Log.Info($"Entity {name} can not give things to {receiverInventory.name}: farther than {radius} m");
                return false;
            }

            return true;
        }
    }
}
