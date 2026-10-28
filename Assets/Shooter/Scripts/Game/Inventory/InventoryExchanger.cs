using System;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Character))]
    public class InventoryExchanger : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float radius = 5f;

        private Character character;
        private Inventory inventory;

        public float Radius => radius;

        public event Action<Character, ItemAsset, int> ReceivedFromCharacter;

        public event Action<ulong> LootOpened;

        private void Awake()
        {
            character = GetComponent<Character>();
            inventory = GetComponent<Inventory>();
        }

        public bool TryGiveStackable(Inventory receiverInventory, StackableItemAsset asset, int amount)
        {
            if (amount <= 0 || !CanReach(receiverInventory)) return false;

            if (inventory.RemoveStackable(asset, amount, Inventory.InventoryShortagePolicy.Rollback) != amount)
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
            if (!CanReach(receiverInventory)) return false;

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

        public bool TryTakeStackable(Inventory sourceInventory, StackableItemAsset asset, int amount)
        {
            if (amount <= 0 || !CanTakeFrom(sourceInventory)) return false;

            if (sourceInventory.RemoveStackable(asset, amount, Inventory.InventoryShortagePolicy.Rollback) != amount)
            {
                Log.Info($"Entity {name} can not take {asset.Id} x {amount} from {sourceInventory.name}: not enough there");
                return false;
            }

            inventory.AddStackable(asset, amount);
            Log.Info($"Entity {name} took {asset.Id} x {amount} from {sourceInventory.name}");
            return true;
        }

        public bool TryTakeUnique(Inventory sourceInventory, int slot)
        {
            if (!CanTakeFrom(sourceInventory)) return false;

            UniqueItem item = sourceInventory.RemoveUniqueAt(slot);
            if (item == null)
            {
                Log.Info($"Entity {name} can not take slot {slot} from {sourceInventory.name}: the slot is empty");
                return false;
            }

            inventory.AddUnique(item);
            Log.Info($"Entity {name} took {item.AssetId} from slot {slot} of {sourceInventory.name}");
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void GiveStackableRpc(long receiverId, FixedString32Bytes stackableId, int amount)
        {
            StackableItemAsset asset = Catalogs.GetCatalog<ItemCatalog>().GetAssetById(stackableId) as StackableItemAsset;
            if (asset == null)
            {
                Log.Info($"Entity {name} failed to rpc give {stackableId} x {amount} to {receiverId} : asset not found");
                return;
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

        public void OpenLoot(Lootable lootable)
        {
            if (!CanTakeFrom(lootable.GetComponent<Inventory>())) return;

            Log.Info($"Entity {name} opens {lootable.name}");
            RaiseLootOpenedRpc(lootable.NetworkObjectId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void TakeStackableRpc(long sourceId, FixedString32Bytes stackableId, int amount)
        {
            StackableItemAsset asset = Catalogs.GetCatalog<ItemCatalog>().GetAssetById(stackableId) as StackableItemAsset;
            if (asset == null)
            {
                Log.Info($"Entity {name} failed to rpc take {stackableId} x {amount} from {sourceId} : asset not found");
                return;
            }

            IncrementalId source = IncrementalId.FindByValue(sourceId, RegisterInactivePolicy.Exclude);
            Inventory sourceInventory = source == null ? null : source.GetComponent<Inventory>();
            if (sourceInventory == null)
            {
                Log.Info($"Entity {name} failed to rpc take {stackableId} x {amount} from {sourceId}: nothing there holds items");
                return;
            }

            if (!TryTakeStackable(sourceInventory, asset, amount))
            {
                Log.Info($"Entity {name} failed to rpc take {stackableId} x {amount} from {sourceId}");
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void TakeUniqueRpc(long sourceId, int slot)
        {
            IncrementalId source = IncrementalId.FindByValue(sourceId, RegisterInactivePolicy.Exclude);
            Inventory sourceInventory = source == null ? null : source.GetComponent<Inventory>();
            if (sourceInventory == null)
            {
                Log.Info($"Entity {name} failed to rpc take slot {slot} from {sourceId}: nothing there holds items");
                return;
            }

            if (!TryTakeUnique(sourceInventory, slot))
            {
                Log.Info($"Entity {name} failed to rpc take slot {slot} from {sourceId}");
            }
        }

        [Rpc(SendTo.Owner)]
        private void RaiseLootOpenedRpc(ulong lootableId)
        {
            Log.Info($"Loot of network object {lootableId} opened");
            LootOpened?.Invoke(lootableId);
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

        private bool CanTakeFrom(Inventory sourceInventory)
        {
            if (!sourceInventory.TryGetComponent(out Lootable _))
            {
                Log.Info($"Entity {name} can not take things from {sourceInventory.name}: it is not lootable");
                return false;
            }

            return CanReach(sourceInventory);
        }

        private bool CanReach(Inventory otherInventory)
        {
            if (otherInventory == inventory || !otherInventory.gameObject.activeInHierarchy)
            {
                Log.Info($"Entity {name} has nobody to exchange things with");
                return false;
            }

            if (Vector3.Distance(otherInventory.transform.position, transform.position) > radius)
            {
                Log.Info($"Entity {name} can not reach {otherInventory.name}: farther than {radius} m");
                return false;
            }

            return true;
        }
    }
}
