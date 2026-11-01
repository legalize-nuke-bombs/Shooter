using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Items/Effects/Heal", fileName = "Heal")]
    public class HealEffect : ItemEffect
    {
        [SerializeField] private float amount = 33f;

        public override void ApplyToUser(GameObject user)
        {
            Health health = user.GetComponent<Health>();

            if (health == null) return;

            health.Heal(amount);
        }

        public override ItemStats Stats => new() { Heal = (int)amount };
    }
}
