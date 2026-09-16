using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(NotificationRecipient))]
    public class LlmNotificationObserver : MonoBehaviour
    {
        private Llm llm;
        private NotificationRecipient recipient;

        private void Awake()
        {
            llm = GetComponent<Llm>();
            recipient = GetComponent<NotificationRecipient>();
        }

        private void OnEnable()
        {
            recipient.Received += Received;
        }

        private void OnDisable()
        {
            recipient.Received -= Received;
        }

        private void Received(NotificationSpec spec)
        {
            IconSpec icon = spec.Icon;
            string iconDescription = icon == null ? "none" : icon.PromptDescription;

            EarSoundSpec sound = spec.Sound;
            string soundDescription = sound == null ? "none" : sound.PromptDescription;

            llm.Notice(
                $"[{Llm.Stamp()}] You have received new notification.\nIcon: {iconDescription}\nSound: {soundDescription}\nText: {spec.Told}",
                spec.Urgent
            );
        }
    }
}
