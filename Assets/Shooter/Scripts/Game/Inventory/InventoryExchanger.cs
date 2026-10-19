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
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private float radius = 10f;

        private Character character;
        private Inventory inventory;

        public float Radius => radius;

        public event Action<Character, ItemAsset, int> ReceivedFromCharacter;

        private void Awake()
        {
            character = GetComponent<Character>();
            inventory = GetComponent<Inventory>();
        }

        public bool TryGiveStackable(Character receiver, StackableItemAsset asset, int amount)
        {
            if (amount <= 0 || !CanGiveTo(receiver, out InventoryExchanger receiverExchanger)) return false;

            if (inventory.RemoveStackable(asset, amount, InventoryShortagePolicy.Rollback) != amount)
            {
                Log.Info($"Entity {name} can not give {asset.Id} x {amount} to {receiver.name}: not enough in the bag");
                return false;
            }

            receiverExchanger.ReceiveStackable(character, asset, amount);
            Log.Info($"Entity {name} gave {asset.Id} x {amount} to {receiver.name}");
            return true;
        }

        public bool TryGiveUnique(Character receiver, int slot)
        {
            if (!CanGiveTo(receiver, out InventoryExchanger receiverExchanger)) return false;

            UniqueItem item = inventory.RemoveUniqueAt(slot);
            if (item == null)
            {
                Log.Info($"Entity {name} can not give slot {slot} to {receiver.name}: the slot is empty");
                return false;
            }

            receiverExchanger.ReceiveUnique(character, item);
            Log.Info($"Entity {name} gave {item.AssetId} from slot {slot} to {receiver.name}");
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void GiveStackableRpc(long receiverId, FixedString32Bytes stackableId, int amount)
        {
            StackableItemAsset asset = Catalogs.GetCatalog<ItemCatalog>().GetAssetById(stackableId) as StackableItemAsset;
            if (asset != null && TryGiveStackable(Character.FindById(receiverId, RegisterInactivePolicy.Exclude), asset, amount)) return;

            Log.Info($"Entity {name} failed to give {stackableId} x {amount} to {receiverId} rpc");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void GiveUniqueRpc(long receiverId, int slot)
        {
            if (TryGiveUnique(Character.FindById(receiverId, RegisterInactivePolicy.Exclude), slot)) return;

            Log.Info($"Entity {name} failed to give slot {slot} to {receiverId} rpc");
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

        private bool CanGiveTo(Character receiver, out InventoryExchanger receiverExchanger)
        {
            receiverExchanger = null;

            if (receiver == null || receiver == character || !receiver.gameObject.activeInHierarchy)
            {
                Log.Info($"Entity {name} has nobody to give things to");
                return false;
            }

            if (Vector3.Distance(receiver.transform.position, transform.position) > radius)
            {
                Log.Info($"Entity {name} can not give things to {receiver.name}: farther than {radius} m");
                return false;
            }

            if (receiver.TryGetComponent(out receiverExchanger)) return true;

            Log.Info($"Entity {name} can not give things to {receiver.name}: it takes nothing");
            return false;
        }
    }
}
