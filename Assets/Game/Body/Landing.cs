using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(Health))]
    public class Landing : MonoBehaviour
    {
        [SerializeField] private float minHeight = 0.6f;

        [SerializeField] private float safeHeight = 3f;

        [SerializeField] private float damagePerMetre = 12f;

        [SerializeField] private SoundSpec sound;

        private Speaker speaker;
        private Health health;

        private void Awake()
        {
            speaker = GetComponent<Speaker>();
            health = GetComponent<Health>();
        }

        public void Land(float height)
        {
            if (height < minHeight) return;

            speaker.Play(sound);

            int damage = Mathf.RoundToInt((height - safeHeight) * damagePerMetre);
            if (damage > 0) health.Damage(damage, null);
        }
    }
}
