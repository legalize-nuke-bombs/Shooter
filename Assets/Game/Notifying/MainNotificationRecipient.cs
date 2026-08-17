using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Notifying
{
    [RequireComponent(typeof(PersistentId))]
    public class MainNotificationRecipient : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private IChildNotificationRecipient[] recipients;

        private void Awake()
        {
            recipients = GetComponentsInChildren<IChildNotificationRecipient>();
            Log.Info($"Entity {this.NameOf()} has {recipients.Length} child notification recipients");
        }

        public void Receive(Notification notification)
        {
            Log.Info($"Entity {this.NameOf()} received notification {notification.Spec}");
            foreach (IChildNotificationRecipient recipient in recipients) recipient.OnReceive(notification);
        }
    }
}
