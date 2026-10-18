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

        [SerializeField] private float radius = 10f;

        private Character character;
        private Inventory inventory;

        public float Radius => radius;

        // Raised by Receive only: picking up, crafting and looting add silently
        public event Action<Character, ItemSpec, int> Received;

        private void Awake()
        {
            character = GetComponent<Character>();
            inventory = GetComponent<Inventory>();
        }

        public bool Give(Character to, StackableItemSpec spec, int amount)
        {
            if (amount <= 0 || !Reaches(to, out InventoryExchanger taker)) return false;

            if (inventory.Remove(spec, amount, InventoryOnConflict.Rollback) != amount)
            {
                Log.Info($"Entity {name} can not give {spec.Id} x {amount} to {to.name}: not enough in the bag");
                return false;
            }

            taker.Receive(character, spec, amount);
            Log.Info($"Entity {name} gave {spec.Id} x {amount} to {to.name}");
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
            Log.Info($"Entity {name} gave {item.SpecId} from slot {slot} to {to.name}");
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void GiveStackableRpc(long targetId, FixedString32Bytes stackableId, int amount)
        {
            var spec = Catalogs.Of<ItemCatalog>().Of(stackableId) as StackableItemSpec;
            if (spec != null && Give(Character.Of(targetId, Inactive.Exclude), spec, amount)) return;

            Log.Info($"Entity {name} failed to give {stackableId} x {amount} to {targetId} rpc");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void GiveUniqueRpc(long targetId, int slot)
        {
            if (Give(Character.Of(targetId, Inactive.Exclude), slot)) return;

            Log.Info($"Entity {name} failed to give slot {slot} to {targetId} rpc");
        }

        private void Receive(Character from, StackableItemSpec spec, int amount)
        {
            inventory.Add(spec, amount);
            Received?.Invoke(from, spec, amount);
        }

        private void Receive(Character from, UniqueItem item)
        {
            inventory.Add(item);
            Received?.Invoke(from, Catalogs.Of<ItemCatalog>().Spec(item.SpecId), 1);
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
