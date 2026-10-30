using System.Text;

namespace Shooter.Game
{
    public struct ItemStats
    {
        public int Heal;
        public int Food;
        public int Intoxication;

        public int Damage;
        public int Magazine;

        public int HealPerHour;

        public static ItemStats operator +(ItemStats left, ItemStats right)
        {
            return new ItemStats
            {
                Heal = left.Heal + right.Heal,
                Food = left.Food + right.Food,
                Intoxication = left.Intoxication + right.Intoxication,

                Damage = left.Damage + right.Damage,
                Magazine = left.Magazine + right.Magazine,

                HealPerHour = left.HealPerHour + right.HealPerHour,
            };
        }

        public string PromptDescription()
        {
            StringBuilder sb = new StringBuilder();

            if (Heal > 0) sb.Append($"Heal {Heal}. ");
            if (Food > 0) sb.Append($"Food {Food}. ");
            if (Intoxication > 0) sb.Append($"Intoxication {Intoxication}. ");

            if (Damage > 0) sb.Append($"Damage {Damage}. ");
            if (Magazine > 0) sb.Append($"Magazine {Magazine}. ");

            if (HealPerHour > 0) sb.Append($"Heal {HealPerHour} per hour. ");

            return sb.ToString();
        }
    }
}
