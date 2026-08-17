using Shooter.Game.Body;
using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Effects/Feed", fileName = "Feed")]
    public class FeedEffect : ItemEffect
    {
        [SerializeField] private float amount = 10f;

        public override void Apply(GameObject user)
        {
            Hunger hunger = user.transform.Find<Hunger>();

            if (hunger == null) return;

            hunger.Restore(amount);
        }

        public override int FoodMarker => (int)amount;
    }
}
