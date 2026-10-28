using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(NotificationRecipient))]
    public class LlmNotificationObserver : MonoBehaviour
    {
        private Llm llm;
        private NotificationRecipient notificationRecipient;

        private void Awake()
        {
            llm = GetComponent<Llm>();
            notificationRecipient = GetComponent<NotificationRecipient>();
        }

        private void OnEnable()
        {
            notificationRecipient.NotificationReceived += HandleNotificationReceived;
        }

        private void OnDisable()
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
