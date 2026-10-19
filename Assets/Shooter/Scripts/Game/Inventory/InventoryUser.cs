using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Speaker))]
    public class InventoryUser : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private Inventory inventory;
        private Speaker speaker;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            speaker = GetComponent<Speaker>();
        }

        public bool Use(StackableItemAsset asset)
        {
            if (!asset.Usable) return false;

            if (inventory.Remove(asset, 1, InventoryOnConflict.Rollback) == 0) return false;

            foreach (ItemEffect effect in asset.Effects)
            {
                effect.Apply(gameObject);
            }

            speaker.PlayForEveryone(asset.UseSound);

            Log.Info($"Entity {name} used one {asset.Id}");
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void UseRpc(FixedString32Bytes stackableId)
        {
            if (Catalogs.GetCatalog<ItemCatalog>().GetAssetById(stackableId) is StackableItemAsset asset && Use(asset)) return;

            Log.Info($"Entity {name} failed to use {stackableId} rpc");
        }
    }
}
