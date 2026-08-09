using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Body.Notifying
{
    public class MainNotificationRecipient : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();
        
        private IChildNotificationRecipient[] recipients;

        public void Awake()
        {
            recipients = GetComponentsInChildren<IChildNotificationRecipient>();
            Log.Info($"Entity {name} has {recipients.Length} child notification recipients");
        }

        public void Receive(Notification notification)
        {
            Log.Info($"Entity {name} received notification");
            foreach (IChildNotificationRecipient recipient in recipients)
            {
                recipient?.OnReceive(notification);
            }
        }
    }
}


