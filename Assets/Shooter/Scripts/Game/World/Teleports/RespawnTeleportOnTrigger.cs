using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Trigger))]
    public class RespawnTeleportOnTrigger : Teleporter
    {
        private static readonly Journal Log = Logs.Here();

        private Trigger trigger;

        private void Awake()
        {
            trigger = GetComponent<Trigger>();
        }

        private void OnEnable()
        {
            trigger.Fired += TeleportCharacterToRespawn;
        }

        private void OnDisable()
        {
            trigger.Fired -= TeleportCharacterToRespawn;
        }

        private void TeleportCharacterToRespawn(Character character)
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
            Teleport(character, movement, destination);
        }
    }
}
