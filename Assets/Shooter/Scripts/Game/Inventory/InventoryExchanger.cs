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

        // Raised by Receive only: picking up, crafting and looting add silently
        public event Action<Character, ItemAsset, int> Received;

        private void Awake()
        {
            character = GetComponent<Character>();
            inventory = GetComponent<Inventory>();
        }

        public bool Give(Character to, StackableItemAsset asset, int amount)
        {
            if (amount <= 0 || !Reaches(to, out InventoryExchanger taker)) return false;

            if (inventory.Remove(asset, amount, InventoryOnConflict.Rollback) != amount)
            {
                Log.Info($"Entity {name} can not give {asset.Id} x {amount} to {to.name}: not enough in the bag");
                return false;
            }

            taker.Receive(character, asset, amount);
            Log.Info($"Entity {name} gave {asset.Id} x {amount} to {to.name}");
            return true;
        }

        public bool Give(Character to, int slot)
        {
            if (!Reaches(to, out InventoryExchanger taker)) return false;

            UniqueItem item = inventory.Remove(slot);
            if (item == null)
            {
                Log.Info($"Entity {name} can not give slot {slot} to {to.name}: the slot is empty");
                return false;
            }

            taker.Receive(character, item);
            Log.Info($"Entity {name} gave {item.AssetId} from slot {slot} to {to.name}");
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void GiveStackableRpc(long targetId, FixedString32Bytes stackableId, int amount)
        {
            var asset = Catalogs.GetCatalog<ItemCatalog>().GetAssetById(stackableId) as StackableItemAsset;
            if (asset != null && Give(Character.FindById(targetId, RegisterInactivePolicy.Exclude), asset, amount)) return;

            Log.Info($"Entity {name} failed to give {stackableId} x {amount} to {targetId} rpc");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void GiveUniqueRpc(long targetId, int slot)
        {
            if (Give(Character.FindById(targetId, RegisterInactivePolicy.Exclude), slot)) return;

            Log.Info($"Entity {name} failed to give slot {slot} to {targetId} rpc");
        }

        private void Receive(Character from, StackableItemAsset asset, int amount)
        {
            inventory.Add(asset, amount);
            Received?.Invoke(from, asset, amount);
        }

        private void Receive(Character from, UniqueItem item)
        {
            inventory.Add(item);
            Received?.Invoke(from, Catalogs.GetCatalog<ItemCatalog>().Asset(item.AssetId), 1);
        }

        private bool Reaches(Character to, out InventoryExchanger taker)
        {
            taker = null;

            if (to == null || to == character || !to.gameObject.activeInHierarchy)
            {
                Log.Info($"Entity {name} has nobody to give things to");
                return false;
            }

            if (Vector3.Distance(to.transform.position, transform.position) > radius)
            {
                Log.Info($"Entity {name} can not give things to {to.name}: farther than {radius} m");
                return false;
            }

            if (to.TryGetComponent(out taker)) return true;

            Log.Info($"Entity {name} can not give things to {to.name}: it takes nothing");
            return false;
        }
    }
}
