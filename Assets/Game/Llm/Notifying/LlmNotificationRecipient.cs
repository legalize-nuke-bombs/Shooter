using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;

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
            IconSpec icon = notification.Icon();
            string iconDescription = icon == null ? "none" : icon.PromptDescription;

            EarSoundSpec sound = notification.Sound();
            string soundDescription = sound == null ? "none" : sound.PromptDescription;

            string told = Template.Filled(notification.Told(), notification);

            llm.Notice(
                $"[{Llm.Stamp()}] You have received new notification.\nIcon: {iconDescription}\nSound: {soundDescription}\nText: {told}",
                notification.Urgent()
            );
        }

        private NotificationSpec Spec(Notification notification)
        {
            NotificationCatalog catalog = Catalogs.Of<NotificationCatalog>();

            if (catalog == null)
            {
                Log.Error(
                    $"Entity {name} has no world to ask about {notification.Spec}, the notification is lost");
                return null;
            }

            return catalog.Of(notification.Spec);
        }
    }
}
