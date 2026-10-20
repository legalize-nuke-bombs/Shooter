using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class RespawnTeleport : ShooterBehaviour, ITriggerable
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private EarSoundAsset sound;

        public void HandleCharacterEntered(Character character)
        {
            Movement movement = character.GetComponent<Movement>();
            if (movement == null) return;

            RespawnPosition respawnPosition = character.GetComponent<RespawnPosition>();
            MainSpawnPoint mainSpawnPoint = respawnPosition == null ? MainSpawnPoint.PickRandom() : null;

            Vector3 destination = respawnPosition != null
                ? respawnPosition.GetPosition()
                : mainSpawnPoint == null
                    ? character.transform.position
                    : mainSpawnPoint.transform.position;

            Log.Info($"Entity {name} teleporting {movement.name} to their respawn position {destination}");
            movement.Teleport(destination);
            movement.GetComponent<EarSpeaker>()?.PlayForOwner(sound);
        }
    }
}
