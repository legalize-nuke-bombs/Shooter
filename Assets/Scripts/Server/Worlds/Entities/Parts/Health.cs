using System;

namespace Shooter.Server.Worlds.Entities.Parts
{
    public sealed class Health : Part
    {
        private readonly int max;
        private int hp;

        public Health(int max)
        {
            this.max = Math.Max(max, 1);
            hp = this.max;
        }

        public int Hp => hp;
        public int MaxHp => max;
        public bool Alive => hp > 0;

        public void Damage(int amount)
        {
            if (Alive && amount > 0)
                hp = Math.Max(hp - amount, 0);
        }

        public void Heal(int amount)
        {
            if (Alive && amount > 0)
                hp = Math.Min(hp + amount, max);
        }
    }
}
