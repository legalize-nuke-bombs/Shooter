using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnMisfire : ShooterComponent
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
            gunner.Misfired += Play;
        }

        protected override void ShooterOnDisable()
        {
            gunner.Misfired -= Play;
        }

        private void Play(FirearmAsset asset)
        {
            speaker.Play(asset.MisfireSound);
            Log.Info($"Entity {name} played the misfire sound of {asset.name}");
        }
    }
}
