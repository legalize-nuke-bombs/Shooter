using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Speaker))]
    public class InventoryUser : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Inventory inventory;
        private Speaker speaker;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            speaker = GetComponent<Speaker>();
        }

        public bool Use(StackableItemSpec spec)
        {
            if (!spec.Usable) return false;

            if (inventory.Remove(spec, 1, InventoryOnConflict.Rollback) == 0) return false;

            foreach (ItemEffect effect in spec.Effects)
            {
                effect.Apply(gameObject);
            }

            speaker.Play(spec.UseSound);

            Log.Info($"Entity {name} used one {spec.Key}");
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void UseRpc(FixedString32Bytes stackableId)
        {
            if (Catalogs.Of<ItemCatalog>().Of(stackableId) is StackableItemSpec spec && Use(spec)) return;

            Log.Info($"Entity {name} failed to use {stackableId} rpc");
        }
    }
}
