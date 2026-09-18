using Shooter.Game.World;
using UnityEngine;

namespace Shooter.Game.Body
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

        [SerializeField] private DamageSpec fallDamage;
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

            speaker.Play(sounds == null ? null : sounds.On(Surface.Under(body)));

            int damage = Mathf.RoundToInt((height - safeHeight) * damagePerMetre);
            if (damage > 0) health.Damage(damage, null, fallDamage);
        }
    }
}
