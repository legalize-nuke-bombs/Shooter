using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class NotificationPublisher : ShooterBehaviour, ITriggerable, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private NotificationAsset notificationAsset;
        [SerializeField] private bool oncePerCharacter = true;

        private HashSet<long> notifiedCharacterIds = new();

        public string ComponentKey => "NotificationPublisher";

        private struct SaveData
        {
            public List<long> Characters { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Characters = notifiedCharacterIds.ToList()
            };
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            notifiedCharacterIds = new HashSet<long>(saveData.Characters);
        }

        public void HandleCharacterEntered(Character character)
        {
            if (notificationAsset == null)
            {
                Log.Error($"Entity {name} does not have a notification asset");
                return;
            }

            NotificationRecipient notificationRecipient = character.GetComponent<NotificationRecipient>();
            if (notificationRecipient == null) return;

            if (oncePerCharacter && !notifiedCharacterIds.Add(character.Id)) return;

            Log.Info($"Entity {name} is sending notification to {notificationRecipient.name}...");
            notificationRecipient.ReceiveNotification(notificationAsset);
        }
    }
}
