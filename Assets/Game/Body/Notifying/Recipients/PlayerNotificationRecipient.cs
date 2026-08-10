using System;
using Shooter.Game.Packing;
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
            if (!IsServer || notification == null) return;

            ShownRpc(new Packed<Notification>(notification));
        }

        [Rpc(SendTo.Owner)]
        private void ShownRpc(Packed<Notification> packed)
        {
            Notification notification = packed.Value;
            if (notification == null) return;

            Log.Info($"Notification {notification.GetType().Name} arrived");

            Shown?.Invoke(notification);
        }
    }
}
