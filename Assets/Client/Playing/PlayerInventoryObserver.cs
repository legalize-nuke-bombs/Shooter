using System;
using Shooter.Client.Interface;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(Inventory))]
    public class PlayerInventoryObserver : NetworkBehaviour, IToastSource
    {
        private const string Stranger = "Незнакомец";

        [SerializeField] private EarSoundSpec sound;

        private readonly NameMapper mapper = new();
        private Inventory inventory;

        public event Action<Toast> Toasted;

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
            // An offline body is owned by the host
            if (item == null || !gameObject.activeInHierarchy) return;

            ReceivedRpc(from == null ? GameObjectRuntimeId.Default : from.Id, item.Id, amount);
        }

        [Rpc(SendTo.Owner)]
        private void ReceivedRpc(long giverId, FixedString32Bytes itemId, int amount)
        {
            ItemCatalog catalog = Catalogs.Of<ItemCatalog>();
            ItemSpec item = catalog == null ? null : catalog.Of(itemId);
            if (item == null) return;

            string named = mapper.Of(giverId);
            string title = item is StackableItemSpec ? $"{item.Title} × {amount}" : item.Title;

            Toasted?.Invoke(new Toast(item.Icon, sound, title, "от " + (string.IsNullOrEmpty(named) ? Stranger : named)));
        }
    }
}
