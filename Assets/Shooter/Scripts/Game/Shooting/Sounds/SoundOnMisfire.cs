using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnMisfire : ShooterBehaviour
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
            gunner.Misfired += Play;
        }

        private void OnDisable()
        {
            gunner.Misfired -= Play;
        }

        private void Play(FirearmAsset asset)
        {
            speaker.Play(asset.MisfireSound);
        }
    }
}
