using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;
using Shooter.Game.World;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Llm))]
    public class LlmNotificationRecipient : MonoBehaviour, IChildNotificationRecipient
    {
        private static readonly Journal Log = Logs.Here();


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

            IconSpec icon = notification.Icon();
            string iconDescription = (icon == null) ? "none" : icon.PromptDescription;

            EarSoundSpec sound = notification.Sound();
            string soundDescription = (sound == null) ? "none" : sound.PromptDescription;

            string told = Template.Filled(spec.Told, notification);

            llm.Notice($"[{Clock.Current.DateTime()}] You have received new notification.\nIcon: {iconDescription}\nSound: {soundDescription}\nText: {told}");
        }

        private NotificationSpec Spec(Notification notification)
        {
            NotificationCatalog catalog = Catalogs.Of<NotificationCatalog>();

            if (catalog == null)
            {
                Log.Error($"Entity {name} has no world to ask about {notification.Spec}, the notification is lost");
                return null;
            }

            return catalog.Of(notification.Spec);
        }
    }
}
