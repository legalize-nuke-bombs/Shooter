using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnShot : ShooterComponent
    {
        private static readonly Journal Log = Logs.Here();

        private Gunner gunner;
        private Speaker speaker;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
            speaker = GetComponent<Speaker>();
        }

        protected override void ShooterOnEnable()
        {
            gunner.Shot += Play;
        }

        protected override void ShooterOnDisable()
        {
            gunner.Shot -= Play;
        }

        private void Play(FirearmAsset asset, ShotHit hit)
        {
            speaker.Play(asset.ShotSound);
            Log.Info($"Entity {name} played the shot sound of {asset.name}");
        }
    }
}
