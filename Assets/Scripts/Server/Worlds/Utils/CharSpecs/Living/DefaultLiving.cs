using System;

namespace Shooter.Server.Worlds.Utils.CharSpecs.Living
{
    public class DefaultLiving : ILiving
    {
        private readonly int maxHp;
        private int hp;
        private bool alive;

        public DefaultLiving(int maxHp)
        {
            this.maxHp = Math.Max(maxHp, 1);
            hp = this.maxHp;
            alive = true;
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
            return alive;
        }

        public void Damage(int amount)
        {
            if (alive && amount > 0)
            {
                hp = Math.Max(hp - amount, 0);
                if (hp == 0)
                {
                    alive = false;
                }
            }
        }

        public void Heal(int amount)
        {
            if (alive && amount > 0)
            {
                hp = Math.Min(hp + amount, maxHp);
            }
        }

        public void Kill()
        {
            hp = 0;
            alive = false;
        }

        public void Resurrect()
        {
            hp = maxHp;
            alive = true;
        }
    }
}
