using Shooter.Game.Body;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Effects/Heal", fileName = "Heal")]
    public class HealEffect : ItemEffect
    {
        [SerializeField] private float amount = 33f;

        public override void Apply(GameObject user)
        {
            Health health = user.GetComponent<Health>();

            if (health == null) return;

            health.Heal(amount);
        }
    }
}
