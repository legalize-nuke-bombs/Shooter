using System;

namespace Shooter.Server.Worlds.Utils.CharSpecs.Living
{
    public class DefaultLiving : ILiving
    {
        private readonly int maxHp;
        private int hp;

        public DefaultLiving(int maxHp)
        {
            this.maxHp = Math.Max(maxHp, 1);
            hp = this.maxHp;
        }

        public int Hp()
        {
            return hp;
        }

        public int MaxHp()
        {
            return maxHp;
        }

        public bool Alive()
        {
            return hp > 0;
        }

        public void Damage(int amount)
        {
            if (Alive() && amount > 0)
            {
                hp = Math.Max(hp - amount, 0);
            }
        }

        public void Heal(int amount)
        {
            if (Alive() && amount > 0)
            {
                hp = Math.Min(hp + amount, maxHp);
            }
        }

        public void Kill()
        {
            hp = 0;
        }

        public void Resurrect()
        {
            hp = maxHp;
        }
    }
}
