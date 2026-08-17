using Shooter.Game.Body;
using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Effects/Heal", fileName = "Heal")]
    public class HealEffect : ItemEffect
    {
        [SerializeField] private float amount = 33f;

        public override void Apply(GameObject user)
        {
            Health health = user.transform.Find<Health>();

            if (health == null) return;

            health.Heal(amount);
        }

        public override int HealMarker => (int)amount;
    }
}
