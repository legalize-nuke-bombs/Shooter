using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    public class Teleport : MonoBehaviour, ITriggerable, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject destination;
        [SerializeField] private EarSoundSpec sound;
        [SerializeField] private bool oncePerCharacter = false;

        private HashSet<long> characters = new HashSet<long>();

        public string ComponentKey => "Teleport";
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
            if (destination == null)
            {
                Log.Warn($"Entity {name} does not have proper destination!");
                return;
            }

            Movement movement = character.GetComponent<Movement>();
            if (movement == null) return;

            if (oncePerCharacter && !characters.Add(character.Value))
            {
                return;
            }

            Vector3 at = destination.transform.position;
            Log.Info($"Entity {name} teleporting {movement.name} to {at}");
            movement.Teleport(at);
            movement.GetComponent<EarSpeaker>()?.Play(sound);
        }
    }
}
