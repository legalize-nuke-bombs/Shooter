using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(NotificationRecipient))]
    public class LlmNotificationObserver : ShooterComponent
    {
        private Llm llm;
        private NotificationRecipient notificationRecipient;

        protected override void ShooterAwake()
        {
            llm = GetComponent<Llm>();
            notificationRecipient = GetComponent<NotificationRecipient>();
        }

        protected override void ShooterOnEnable()
        {
            notificationRecipient.NotificationReceived += HandleNotificationReceived;
        }

        protected override void ShooterOnDisable()
        {
            notificationRecipient.NotificationReceived -= HandleNotificationReceived;
        }

        private void HandleNotificationReceived(NotificationAsset notification)
        {
            IconAsset icon = notification.Icon;
            string iconDescription = icon == null ? "none" : icon.PromptDescription;

            EarSoundAsset sound = notification.Sound;
            string soundDescription = sound == null ? "none" : sound.PromptDescription;

            llm.AppendEvent($"[{Llm.FormatCurrentWorldTime()}] You have received new notification.\nIcon: {iconDescription}\nSound: {soundDescription}\nText: {notification.PromptText}", notification.IsUrgent);
        }
    }
}
