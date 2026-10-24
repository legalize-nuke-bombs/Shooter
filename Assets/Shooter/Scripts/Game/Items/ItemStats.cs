namespace Shooter.Game
{
    // The numbers of a thing: each effect gives its own, an item sums those of its effects
    public struct ItemStats
    {
        public int Heal;
        public int HealPerMinute;
        public int Food;
        public int Intoxication;
        public int Damage;
        public int Magazine;

        public static ItemStats operator +(ItemStats left, ItemStats right)
        {
            return new ItemStats
            {
                Heal = left.Heal + right.Heal,
                HealPerMinute = left.HealPerMinute + right.HealPerMinute,
                Food = left.Food + right.Food,
                Intoxication = left.Intoxication + right.Intoxication,
                Damage = left.Damage + right.Damage,
                Magazine = left.Magazine + right.Magazine
            };
        }
    }
}
