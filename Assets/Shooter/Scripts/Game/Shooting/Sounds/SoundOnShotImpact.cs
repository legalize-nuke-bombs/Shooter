using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    public class SoundOnShotImpact : ShooterComponent
    {
        private static readonly Journal Log = Logs.Here();

        private Gunner gunner;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
        }

        protected override void ShooterOnEnable()
        {
            gunner.ShotTraced += Play;
        }

        protected override void ShooterOnDisable()
        {
            gunner.ShotTraced -= Play;
        }

        private void Play(ShotTrace shot)
        {
            if (shot.ImpactSound == null)
            {
                Log.Info($"Entity {name} played no impact sound: its shot ended at {shot.End} on nothing that has one");
                return;
            }

            WorldSpeaker.PlayLocallyAt(shot.ImpactSound, shot.ImpactSoundVariant, shot.End);
            Log.Info($"Entity {name} played the impact sound {shot.ImpactSound.name} at {shot.End}");
        }
    }
}
