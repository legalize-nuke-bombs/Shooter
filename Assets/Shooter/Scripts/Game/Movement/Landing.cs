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
        private CharacterController body;
        private Health health;

        private Speaker speaker;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            speaker = GetComponent<Speaker>();
            health = GetComponent<Health>();
        }

        public void Land(float height)
        {
            if (height < minHeight) return;

            speaker.PlayForEveryone(sounds == null ? null : sounds.GetSoundOn(Surface.FindUnder(body)), gain);

            int damage = Mathf.RoundToInt((height - safeHeight) * damagePerMetre);
            if (damage > 0) health.Damage(damage, null, fallDamage);
        }
    }
}
