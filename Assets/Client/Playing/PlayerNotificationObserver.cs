using Shooter.Client.Interface;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Client.Playing
{
    // A static notification shows up in the corner of the owner's screen as its asset describes it
    [RequireComponent(typeof(NotificationRecipient))]
    public class PlayerNotificationObserver : NetworkBehaviour
    {
        private NotificationRecipient recipient;

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
            // A switched-off body has nobody to show it to
            if (!gameObject.activeInHierarchy) return;

            ShownRpc(spec.Id);
        }

        [Rpc(SendTo.Owner)]
        private void ShownRpc(FixedString32Bytes specId)
        {
            NotificationCatalog catalog = Catalogs.Of<NotificationCatalog>();
            NotificationSpec spec = catalog == null ? null : catalog.Of(specId);
            NotificationOverlay feed = NotificationOverlay.Current;
            if (spec == null || feed == null) return;

            feed.Show(new Toast(spec.Icon, spec.Sound, spec.Title, spec.Subtitle));
        }
    }
}
