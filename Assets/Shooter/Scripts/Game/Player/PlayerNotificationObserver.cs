using System;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NotificationRecipient))]
    public class PlayerNotificationObserver : ShooterNetworkComponent, IToastSource
    {
        private NotificationRecipient notificationRecipient;

        public event Action<Toast> ToastRaised;

        protected override void ShooterAwake()
        {
            notificationRecipient = GetComponent<NotificationRecipient>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) notificationRecipient.NotificationReceived += HandleNotificationReceived;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) notificationRecipient.NotificationReceived -= HandleNotificationReceived;
        }

        private void HandleNotificationReceived(NotificationAsset notification)
        {
            // An offline body is switched off and owned by the host, who must not get its toasts
            if (!gameObject.activeInHierarchy) return;

            RaiseNotificationToastRpc(notification.NetworkId);
        }

        [Rpc(SendTo.Owner)]
        private void RaiseNotificationToastRpc(FixedString32Bytes notificationId)
        {
            NotificationCatalog notificationCatalog = Catalogs.GetCatalog<NotificationCatalog>();
            NotificationAsset notification = notificationCatalog == null ? null : notificationCatalog.GetAssetById(notificationId);
            if (notification == null) return;

            ToastRaised?.Invoke(new Toast(notification.Icon, notification.Sound, notification.Title, notification.Subtitle));
        }
    }
}
