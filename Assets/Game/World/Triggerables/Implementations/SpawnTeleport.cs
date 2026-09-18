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

            SpawnPoint spawnPoint = character.GetComponent<SpawnPoint>();
            MainSpawnPoint point = spawnPoint == null ? MainSpawnPoint.Pick() : null;

            Vector3 destination = spawnPoint != null
                ? spawnPoint.GetPosition()
                : point == null
                    ? character.transform.position
                    : point.transform.position;

            Log.Info($"Entity {name} teleporting {movement.name} to their spawn point {destination}");
            movement.Teleport(destination);
            movement.GetComponent<EarSpeaker>()?.Play(sound);
        }
    }
}
