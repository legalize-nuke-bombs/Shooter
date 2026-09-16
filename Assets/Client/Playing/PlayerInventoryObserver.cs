using Shooter.Client.Interface;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Client.Playing
{
    // Things handed over to the player show up in the corner: what, how many and from whom
    [RequireComponent(typeof(Inventory))]
    public class PlayerInventoryObserver : NetworkBehaviour
    {
        private const string Stranger = "Незнакомец";

        [SerializeField] private EarSoundSpec sound;

        private readonly NameMapper mapper = new();
        private Inventory inventory;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) inventory.Received += Relay;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) inventory.Received -= Relay;
        }

        private void Relay(Character from, ItemSpec item, int amount)
        {
            // A switched-off body has nobody to show it to
            if (item == null || !gameObject.activeInHierarchy) return;

            ReceivedRpc(from == null ? GameObjectRuntimeId.Default : from.Id, item.Id, amount);
        }

        [Rpc(SendTo.Owner)]
        private void ReceivedRpc(long giverId, FixedString32Bytes itemId, int amount)
        {
            NotificationOverlay feed = NotificationOverlay.Current;
            ItemCatalog catalog = Catalogs.Of<ItemCatalog>();
            ItemSpec item = catalog == null ? null : catalog.Of(itemId);
            if (feed == null || item == null) return;

            string named = mapper.Of(giverId);
            string title = item is StackableItemSpec ? $"{item.Title} × {amount}" : item.Title;

            feed.Show(new Toast(item.Icon, sound, title, "от " + (string.IsNullOrEmpty(named) ? Stranger : named)));
        }
    }
}
