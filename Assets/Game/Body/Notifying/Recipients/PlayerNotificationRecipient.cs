using System;
using Shooter.Logging;
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
            if (!IsServer || notification.IsEmpty) return;

            ShownRpc(notification);
        }

        [Rpc(SendTo.Owner)]
        private void ShownRpc(Notification notification)
        {
            Log.Info($"Notification {notification.Spec} arrived");

            Shown?.Invoke(notification);
        }
    }
}
