using System;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body.Notifying
{
    [RequireComponent(typeof(MainNotificationRecipient))]
    public class PlayerNotificationRecipient : NetworkBehaviour, IChildNotificationRecipient
    {
        private static readonly Journal Log = Logs.Here();

        public event Action<Notification> Shown;

        public void OnReceive(Notification notification)
        {
            if (!IsServer) return;

            FixedString4096Bytes packed = NotificationPacking.Pack(notification);
            if (packed.IsEmpty) return;

            ShownRpc(packed);
        }

        [Rpc(SendTo.Owner)]
        private void ShownRpc(FixedString4096Bytes packed)
        {
            Notification notification = NotificationPacking.Unpack(packed);
            if (notification == null) return;

            Log.Info($"Notification {notification.GetType().Name} arrived");

            Shown?.Invoke(notification);
        }
    }
}
