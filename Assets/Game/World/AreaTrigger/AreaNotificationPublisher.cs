using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    public class AreaNotificationPublisher : AreaTrigger
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private NotificationSpec notificationSpec;

        protected override void OnTrigger(PersistentId character)
        {
            if (notificationSpec == null)
            {
                Log.Warn($"Entity {name} does not have a notification spec");
                return;
            }

            MainNotificationRecipient notificationRecipient = character.GetComponent<MainNotificationRecipient>();
            if (notificationRecipient == null) return;

            Log.Info($"Entity {name} is sending notification to {notificationRecipient.name}...");

            notificationRecipient.Receive(notificationSpec.Notify());
        }
    }
}
