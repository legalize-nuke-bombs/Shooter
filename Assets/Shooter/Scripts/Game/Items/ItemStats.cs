namespace Shooter.Game
{
    public struct ItemStats
    {
        public int Heal;
        public int HealPerHour;
        public int Food;
        public int Intoxication;
        public int Damage;
        public int Magazine;

        public static ItemStats operator +(ItemStats left, ItemStats right)
        {
            return new ItemStats
            {
                Heal = left.Heal + right.Heal,
                HealPerHour = left.HealPerHour + right.HealPerHour,
                Food = left.Food + right.Food,
                Intoxication = left.Intoxication + right.Intoxication,
                Damage = left.Damage + right.Damage,
                Magazine = left.Magazine + right.Magazine
            };
        }
    }
}
