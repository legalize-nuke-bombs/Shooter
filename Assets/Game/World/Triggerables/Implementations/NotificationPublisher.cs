using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    public class NotificationPublisher : MonoBehaviour, ITriggerable, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private NotificationSpec notificationSpec;
        [SerializeField] private bool oncePerCharacter = true;

        private HashSet<long> characters = new HashSet<long>();

        public string ComponentKey => "NotificationPublisher";
        private struct SaveData
        {
            public List<long> Characters { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Characters = characters.ToList()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            characters = new HashSet<long>(sd.Characters);
        }

        public void OnTrigger(Character character)
        {
            if (notificationSpec == null)
            {
                Log.Warn($"Entity {name} does not have a notification spec");
                return;
            }

            MainNotificationRecipient notificationRecipient = character.GetComponent<MainNotificationRecipient>();
            if (notificationRecipient == null) return;

            if (oncePerCharacter && !characters.Add(character.Id))
            {
                return;
            }

            Log.Info($"Entity {name} is sending notification to {notificationRecipient.name}...");
            notificationRecipient.Receive(notificationSpec.Notify());
        }
    }
}
