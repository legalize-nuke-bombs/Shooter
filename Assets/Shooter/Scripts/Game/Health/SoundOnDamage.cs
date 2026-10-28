using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EarSpeaker))]
    public class SoundOnDamage : MonoBehaviour
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
            health.Damaged += Play;
        }

        private void OnDisable()
        {
            health.Damaged -= Play;
        }

        private void Play(double amount, Character attacker, DamageAsset damageType)
        {
            earSpeaker.PlayForOwner(sound);
        }
    }
}
