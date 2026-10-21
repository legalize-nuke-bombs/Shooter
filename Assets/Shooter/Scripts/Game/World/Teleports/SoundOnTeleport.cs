using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Teleporter))]
    public class SoundOnTeleport : ShooterBehaviour
    {
        [SerializeField] private EarSoundAsset sound;

        private Teleporter teleporter;

        protected override void ShooterAwake()
        {
            teleporter = GetComponent<Teleporter>();
        }

        private void OnEnable()
        {
            teleporter.Teleported += PlayForTeleported;
        }

        private void OnDisable()
        {
            teleporter.Teleported -= PlayForTeleported;
        }

        private void PlayForTeleported(Character character)
        {
            character.GetComponent<EarSpeaker>()?.PlayForOwner(sound);
        }
    }
}
