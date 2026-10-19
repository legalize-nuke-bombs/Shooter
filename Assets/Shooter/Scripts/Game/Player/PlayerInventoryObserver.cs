using System;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(InventoryExchanger))]
    public class PlayerInventoryObserver : NetworkBehaviour, IToastSource
    {
        private const string Stranger = "Незнакомец";

        [SerializeField] private EarSoundAsset sound;

        private readonly NameMapper nameMapper = new();
        private InventoryExchanger inventoryExchanger;

        public event Action<Toast> ToastRaised;

        private void Awake()
        {
            inventoryExchanger = GetComponent<InventoryExchanger>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) inventoryExchanger.ReceivedFromCharacter += HandleReceivedFromCharacter;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) inventoryExchanger.ReceivedFromCharacter -= HandleReceivedFromCharacter;
        }

        private void HandleReceivedFromCharacter(Character giver, ItemAsset item, int amount)
        {
            // An offline body is switched off and owned by the host, who must not get its toasts
            if (item == null || !gameObject.activeInHierarchy) return;

            RaiseReceivedItemToastRpc(giver == null ? CharacterId.None : giver.Id, item.NetworkId, amount);
        }

        [Rpc(SendTo.Owner)]
        private void RaiseReceivedItemToastRpc(long giverId, FixedString32Bytes itemId, int amount)
        {
            ItemCatalog itemCatalog = Catalogs.GetCatalog<ItemCatalog>();
            ItemAsset item = itemCatalog == null ? null : itemCatalog.GetAssetById(itemId);
            if (item == null) return;

            string giverName = nameMapper.GetShownNameById(giverId);
            string title = item is StackableItemAsset ? $"{item.Title} × {amount}" : item.Title;

            ToastRaised?.Invoke(new Toast(item.Icon, sound, title, "от " + (string.IsNullOrEmpty(giverName) ? Stranger : giverName)));
        }
    }
}
