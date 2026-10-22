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
            gunner.ShotTraced += Play;
        }

        private void OnDisable()
        {
            gunner.ShotTraced -= Play;
        }

        private void Play(ShotTrace shot)
        {
            PointSounds.Play(shot.ImpactSound, shot.ImpactSoundVariant, shot.End);
        }
    }
}
