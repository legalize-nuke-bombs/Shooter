using Shooter.Game.Body.Notifying;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.Notifying
{
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(MainNotificationRecipient))]
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
            string told = Told(notification);

            if (told == null)
            {
                Log.Info($"Entity {name} has nothing to remember about {notification.GetType().Name}");
                return;
            }

            llm.Notice($"[{Environment.Current.Clock.DateTime()}] {told}");
        }

        private static string Told(Notification notification)
        {
            return notification switch
            {
                ItemsGivenNotification given => $"Character {given.ActorId} gave you {given.ItemSpecId} x {given.Amount}",
                MailNotification mail => $"Mail from {mail.SenderId}: {mail.Content}",
                _ => null
            };
        }
    }
}
