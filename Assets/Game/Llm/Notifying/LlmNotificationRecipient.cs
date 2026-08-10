using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(MainNotificationRecipient))]
    public class LlmNotificationRecipient : MonoBehaviour, IChildNotificationRecipient
    {
        private static readonly Journal Log = Logs.Here();

        private readonly LlmNames names = new LlmNames();

        private Llm llm;

        private void Awake()
        {
            llm = GetComponent<Llm>();
        }

        public void OnReceive(Notification notification)
        {
            NotificationSpec spec = Spec(notification);
            if (spec == null) return;

            if (string.IsNullOrEmpty(spec.Told))
            {
                Log.Info($"Entity {name} has nothing to remember about {notification.Spec}");
                return;
            }

            string told = Template.Filled(spec.Told, notification, names);

            llm.Notice($"[{Environment.Current.Clock.DateTime()}] {told}");
        }

        private NotificationSpec Spec(Notification notification)
        {
            NotificationCatalog catalog = Environment.Current == null ? null : Environment.Current.Notifications;

            if (catalog == null)
            {
                Log.Error($"Entity {name} has no world to ask about {notification.Spec}, the notification is lost");
                return null;
            }

            return catalog.Of(notification.Spec);
        }
    }
}
