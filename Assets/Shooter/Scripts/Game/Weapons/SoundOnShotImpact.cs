using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Gunner))]
    public class SoundOnShotImpact : ShooterBehaviour
    {
        private Gunner gunner;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
        }

        private void OnEnable()
        {
            gunner.ShotSeen += Play;
        }

        private void OnDisable()
        {
            gunner.ShotSeen -= Play;
        }

        private void Play(SeenShot shot)
        {
            PointSounds.Play(shot.ImpactSound, shot.ImpactSoundVariant, shot.End);
        }
    }
}
