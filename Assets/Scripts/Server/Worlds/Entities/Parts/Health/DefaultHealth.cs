using System;
using Shooter.Logging;

namespace Shooter.Server.Worlds.Entities.Parts.Health
{
    public sealed class DefaultHealth : Health
    {
        private readonly int max;
        private int hp;

        public DefaultHealth(int max)
        {
            this.max = Math.Max(max, 1);
            hp = this.max;
        }

        public override int Hp => hp;
        public override int MaxHp => max;
        public override bool Alive => hp > 0;

        public override void Damage(int amount)
        {
            if (Alive && amount > 0)
            {
                Log.Info("DefaultHealth part damaged amount {}", amount);
                hp = Math.Max(hp - amount, 0);
            }
            else
            {
                Log.Info("DefaultHealth part can not be damaged because it is already dead");
            }
        }

        public override void Heal(int amount)
        {
            if (Alive && amount > 0)
            {
                Log.Info("DefaultHealth part healed amount {}", amount);
                hp = Math.Min(hp + amount, max);
            }
            else
            {
                Log.Info("DefaultHealth part can not be healed because it is dead");
            }
        }
    }
}
