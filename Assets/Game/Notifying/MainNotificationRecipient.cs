using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Notifying
{
    public class MainNotificationRecipient : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();
        
        private IChildNotificationRecipient[] recipients;

        private void Awake()
        {
            recipients = GetComponentsInChildren<IChildNotificationRecipient>();
            Log.Info($"Entity {name} has {recipients.Length} child notification recipients");
        }

        public void Receive(Notification notification)
        {
            Log.Info($"Entity {name} received notification {notification.Spec}");
            foreach (IChildNotificationRecipient recipient in recipients)
            {
                recipient.OnReceive(notification);
            }
        }
    }
}


