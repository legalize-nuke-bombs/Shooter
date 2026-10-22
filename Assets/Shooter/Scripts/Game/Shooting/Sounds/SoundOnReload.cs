using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnReload : ShooterBehaviour
    {
        private Gunner gunner;
        private Speaker speaker;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
            speaker = GetComponent<Speaker>();
        }

        private void OnEnable()
        {
            gunner.ReloadStarted += Play;
        }

        private void OnDisable()
        {
            gunner.ReloadStarted -= Play;
        }

        private void Play(FirearmAsset asset)
        {
            speaker.Play(asset.ReloadSound);
        }
    }
}
