using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EarSpeaker))]
    public class SoundOnDeath : MonoBehaviour
    {
        [SerializeField] private EarSoundAsset sound;

        private Health health;
        private EarSpeaker earSpeaker;

        private void Awake()
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
