using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Sleeper))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnWakeUp : ShooterBehaviour
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
            sleeper.WokeUp += Play;
        }

        protected override void ShooterOnDisable()
        {
            sleeper.WokeUp -= Play;
        }

        private void Play()
        {
            speaker.Play(sound);
        }
    }
}
