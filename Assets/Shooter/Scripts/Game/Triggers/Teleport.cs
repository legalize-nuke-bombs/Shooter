using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class Teleport : MonoBehaviour, ITriggerable, ISaveableComponent
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private GameObject destination;
        [SerializeField] private EarSoundAsset sound;
        [SerializeField] private bool oncePerCharacter = false;

        private HashSet<long> teleportedCharacterIds = new();

        public string ComponentKey => "Teleport";

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

        public void HandleCharacterEntered(Character character)
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
            movement.Teleport(destinationPosition);
            movement.GetComponent<EarSpeaker>()?.PlayForOwner(sound);
        }
    }
}
