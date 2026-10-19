using UnityEngine;

namespace Shooter.Game
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

        private void Received(NotificationAsset asset)
        {
            IconAsset icon = asset.Icon;
            string iconDescription = icon == null ? "none" : icon.PromptDescription;

            EarSoundAsset sound = asset.Sound;
            string soundDescription = sound == null ? "none" : sound.PromptDescription;

            llm.Notice(
                $"[{Llm.Stamp()}] You have received new notification.\nIcon: {iconDescription}\nSound: {soundDescription}\nText: {asset.Told}",
                asset.Urgent
            );
        }
    }
}
