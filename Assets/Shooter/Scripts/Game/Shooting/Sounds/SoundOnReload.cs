using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnReload : ShooterComponent
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
            gunner.ReloadStarted += Play;
        }

        protected override void ShooterOnDisable()
        {
            gunner.ReloadStarted -= Play;
        }

        private void Play(FirearmAsset asset)
        {
            speaker.Play(asset.ReloadSound);
            Log.Info($"Entity {name} played the reload sound of {asset.name}");
        }
    }
}
