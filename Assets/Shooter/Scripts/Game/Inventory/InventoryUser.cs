using System;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    public class InventoryUser : ShooterNetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Inventory inventory;

        public event Action<StackableItemAsset> Used;

        protected override void ShooterAwake()
        {
            inventory = GetComponent<Inventory>();
        }

        public bool TryUseItem(StackableItemAsset asset)
        {
            if (!asset.IsUsable) return false;

            if (inventory.RemoveStackable(asset, 1, InventoryShortagePolicy.Rollback) == 0) return false;

            foreach (ItemEffect effect in asset.Effects)
            {
                effect.ApplyToUser(gameObject);
            }

            Log.Info($"Entity {name} used one {asset.Id}");
            Used?.Invoke(asset);
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void UseItemRpc(FixedString32Bytes stackableId)
        {
            if (Catalogs.GetCatalog<ItemCatalog>().GetAssetById(stackableId) is StackableItemAsset asset && TryUseItem(asset)) return;

            Log.Info($"Entity {name} failed to use {stackableId} rpc");
        }
    }
}
