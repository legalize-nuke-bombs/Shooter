using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Sleeper))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnFallAsleep : ShooterBehaviour
    {
        [SerializeField] private SoundAsset sound;

        private Sleeper sleeper;
        private Speaker speaker;

        protected override void ShooterAwake()
        {
            sleeper = GetComponent<Sleeper>();
            speaker = GetComponent<Speaker>();
        }

        private void OnEnable()
        {
            sleeper.FellAsleep += Play;
        }

        private void OnDisable()
        {
            sleeper.FellAsleep -= Play;
        }

        private void Play()
        {
            speaker.Play(sound);
        }
    }
}
