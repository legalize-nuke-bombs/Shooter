using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Trigger))]
    public class TeleportOnTrigger : Teleporter, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject destination;
        [SerializeField] private bool oncePerCharacter = false;

        private HashSet<long> teleportedCharacterIds = new();
        private Trigger trigger;

        private struct SaveData
        {
            public List<long> Characters { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Characters = teleportedCharacterIds.ToList()
            };
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            teleportedCharacterIds = new HashSet<long>(saveData.Characters);
        }

        protected override void ShooterAwake()
        {
            trigger = GetComponent<Trigger>();
        }

        protected override void ShooterOnEnable()
        {
            trigger.Fired += TeleportCharacter;
        }

        protected override void ShooterOnDisable()
        {
            trigger.Fired -= TeleportCharacter;
        }

        private void TeleportCharacter(Character character)
        {
            if (destination == null)
            {
                Log.Error($"Entity {name} does not have proper destination!");
                return;
            }

            Movement movement = character.GetComponent<Movement>();
            if (movement == null) return;

            if (oncePerCharacter && !teleportedCharacterIds.Add(character.Id)) return;

            Vector3 destinationPosition = destination.transform.position;
            Log.Info($"Entity {name} teleporting {movement.name} to {destinationPosition}");
            Teleport(character, movement, destinationPosition);
        }
    }
}
