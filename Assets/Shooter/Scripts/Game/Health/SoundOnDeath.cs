using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EarSpeaker))]
    public class SoundOnDeath : ShooterBehaviour
    {
        [SerializeField] private EarSoundAsset sound;

        private Health health;
        private EarSpeaker earSpeaker;

        protected override void ShooterAwake()
        {
            health = GetComponent<Health>();
            earSpeaker = GetComponent<EarSpeaker>();
        }

        private void OnEnable()
        {
            health.Died += Play;
        }

        private void OnDisable()
        {
            health.Died -= Play;
        }

        private void Play()
        {
            earSpeaker.PlayForOwner(sound);
        }
    }
}
