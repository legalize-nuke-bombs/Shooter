using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    public class NotificationPublisher : MonoBehaviour, ITriggerable
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private NotificationSpec notificationSpec;

        public void OnTrigger(PersistentId character)
        {
            if (notificationSpec == null)
            {
                Log.Warn($"Entity {this.NameOf()} does not have a notification spec");
                return;
            }

            MainNotificationRecipient notificationRecipient = character.GetComponent<MainNotificationRecipient>();
            if (notificationRecipient == null) return;

            Log.Info($"Entity {this.NameOf()} is sending notification to {notificationRecipient.name}...");

            notificationRecipient.Receive(notificationSpec.Notify());
        }
    }
}
