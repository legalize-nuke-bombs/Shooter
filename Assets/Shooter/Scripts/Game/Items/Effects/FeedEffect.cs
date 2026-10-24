using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Effects/Feed", fileName = "Feed")]
    public class FeedEffect : ItemEffect
    {
        [SerializeField] private float amount = 10f;

        public override void ApplyToUser(GameObject user)
        {
            Hunger hunger = user.GetComponent<Hunger>();

            if (hunger == null) return;

            hunger.RestoreAmount(amount);
        }

        public override ItemStat Stat => new("Сытость", $"+{FoodAmount}", $"Food {FoodAmount}");

        public override int FoodAmount => (int)amount;
    }
}
