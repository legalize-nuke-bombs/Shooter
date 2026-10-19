using System;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(NotificationRecipient))]
    public class PlayerNotificationObserver : NetworkBehaviour, IToastSource
    {
        private NotificationRecipient recipient;

        public event Action<Toast> Toasted;

        private void Awake()
        {
            recipient = GetComponent<NotificationRecipient>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) recipient.Received += Relay;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) recipient.Received -= Relay;
        }

        private void Relay(NotificationAsset asset)
        {
            // An offline body is owned by the host
            if (!gameObject.activeInHierarchy) return;

            ShownRpc(asset.NetworkId);
        }

        [Rpc(SendTo.Owner)]
        private void ShownRpc(FixedString32Bytes assetId)
        {
            NotificationCatalog catalog = Catalogs.Of<NotificationCatalog>();
            NotificationAsset asset = catalog == null ? null : catalog.Of(assetId);
            if (asset == null) return;

            Toasted?.Invoke(new Toast(asset.Icon, asset.Sound, asset.Title, asset.Subtitle));
        }
    }
}
