using Unity.Netcode;
using UnityEngine;
using Shooter.Logging;

namespace Shooter.Game.Items
{
    [RequireComponent(typeof(Inventory))]
    public class Loadout : NetworkBehaviour
    {
        [SerializeField] private Item[] items;

        public override void OnNetworkSpawn()
        {
            if (!IsServer || items.Length == 0) return;

            var inventory = GetComponent<Inventory>();

            foreach (Item item in items)
                inventory.Add(item);

            Log.Info("Entity {} spawned with {} items in the bag", name, items.Length);
        }
    }
}
