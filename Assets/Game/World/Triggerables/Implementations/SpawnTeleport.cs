using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    public class SpawnTeleport : MonoBehaviour, ITriggerable
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private EarSoundSpec sound;

        public void OnTrigger(Character character)
        {
            Movement movement = character.GetComponent<Movement>();
            if (movement == null) return;

            Sleeper sleeper = character.GetComponent<Sleeper>();

            Vector3 destination = sleeper == null
                ? (MainSpawnPoint.Current == null ? transform.position : MainSpawnPoint.Current.transform.position)
                : sleeper.SpawnPoint;

            Log.Info($"Entity {name} teleporting {movement.name} to their spawn point {destination}");
            movement.Teleport(destination);
            movement.GetComponent<EarSpeaker>()?.Play(sound);
        }
    }
}
