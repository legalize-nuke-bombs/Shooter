using Shooter.Game.Body;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Effects/Feed", fileName = "Feed")]
    public class FeedEffect : ItemEffect
    {
        [SerializeField] private float amount = 10f;

        public override int FoodMarker => (int)amount;

        public override void Apply(GameObject user)
        {
            Hunger hunger = user.GetComponent<Hunger>();

            if (hunger == null) return;

            hunger.Restore(amount);
        }
    }
}
