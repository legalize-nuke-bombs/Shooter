using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Sleeper))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnFallAsleep : ShooterComponent
    {
        [SerializeField] private SoundAsset sound;

        private Sleeper sleeper;
        private Speaker speaker;

        protected override void ShooterAwake()
        {
            sleeper = GetComponent<Sleeper>();
            speaker = GetComponent<Speaker>();
        }

        protected override void ShooterOnEnable()
        {
            sleeper.FellAsleep += Play;
        }

        protected override void ShooterOnDisable()
        {
            sleeper.FellAsleep -= Play;
        }

        private void Play()
        {
            speaker.Play(sound);
        }
    }
}
