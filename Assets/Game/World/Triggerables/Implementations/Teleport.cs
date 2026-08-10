using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    public class Teleport : MonoBehaviour, ITriggerable
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject destination;
        [SerializeField] private EarSoundSpec sound = null;

        public void OnTrigger(PersistentId character)
        {
            if (destination == null)
            {
                Log.Warn($"Entity {name} does not have proper destination!");
                return;
            }

            Movement movement = character.GetComponent<Movement>();
            if (movement == null) return;

            Vector3 at = destination.transform.position;

            Log.Info($"Entity {name} teleporting {movement.name} to {at}");

            movement.Teleport(at);
            movement.GetComponent<EarSpeaker>()?.Play(sound);
        }
    }
}
