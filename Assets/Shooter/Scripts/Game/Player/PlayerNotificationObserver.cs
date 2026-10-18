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

        private void Relay(NotificationSpec spec)
        {
            // An offline body is owned by the host
            if (!gameObject.activeInHierarchy) return;

            ShownRpc(spec.NetworkId);
        }

        [Rpc(SendTo.Owner)]
        private void ShownRpc(FixedString32Bytes specId)
        {
            NotificationCatalog catalog = Catalogs.Of<NotificationCatalog>();
            NotificationSpec spec = catalog == null ? null : catalog.Of(specId);
            if (spec == null) return;

            Toasted?.Invoke(new Toast(spec.Icon, spec.Sound, spec.Title, spec.Subtitle));
        }
    }
}
