using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Character))]
    public class NotificationRecipient : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public event Action<NotificationAsset> NotificationReceived;

        public void ReceiveNotification(NotificationAsset notification)
        {
            if (notification == null) return;

            Log.Info($"Entity {name} received notification {notification.Id}");
            NotificationReceived?.Invoke(notification);
        }
    }
}
