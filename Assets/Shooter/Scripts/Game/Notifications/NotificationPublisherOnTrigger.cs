using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Trigger))]
    public class NotificationPublisherOnTrigger : ShooterBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private NotificationAsset notificationAsset;
        [SerializeField] private bool oncePerCharacter = true;

        private HashSet<long> notifiedCharacterIds = new();
        private Trigger trigger;

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

        protected override void ShooterAwake()
        {
            trigger = GetComponent<Trigger>();
        }

        private void OnEnable()
        {
            trigger.Fired += Notify;
        }

        private void OnDisable()
        {
            trigger.Fired -= Notify;
        }

        private void Notify(Character character)
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
