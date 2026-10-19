using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(CharacterController))]
    public class Landing : MonoBehaviour
    {
        [SerializeField] private float minHeight = 0.6f;
        [SerializeField] private float safeHeight = 3f;
        [SerializeField] private float damagePerMetre = 12f;
        [SerializeField] private SurfaceSounds sounds;
        [SerializeField] private float gain = 1.7f;
        [SerializeField] private DamageAsset fallDamage;

        private CharacterController characterController;
        private Health health;
        private Speaker speaker;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            speaker = GetComponent<Speaker>();
            health = GetComponent<Health>();
        }

        public void HandleLanding(float fallHeight)
        {
            if (fallHeight < minHeight) return;

            speaker.PlayForEveryone(sounds == null ? null : sounds.GetSoundOn(Surface.FindUnder(characterController)), gain);

            int damage = Mathf.RoundToInt((fallHeight - safeHeight) * damagePerMetre);
            if (damage > 0) health.TakeDamage(damage, null, fallDamage);
        }
    }
}
