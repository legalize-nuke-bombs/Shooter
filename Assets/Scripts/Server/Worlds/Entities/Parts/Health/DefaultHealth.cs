using System;

namespace Shooter.Server.Worlds.Entities.Parts.Health
{
    public sealed class DefaultHealth : Health
    {
        private readonly int max;
        private int hp;

        public DefaultHealth(Entity self, int max) : base(self)
        {
            this.max = Math.Max(max, 1);
            hp = this.max;
        }

        public override int Hp => hp;
        public override int MaxHp => max;
        public override bool Alive => hp > 0;

        public override void Damage(int amount)
        {
            if (!Alive || amount <= 0) return;

            hp = Math.Max(hp - amount, 0);
            if (!Alive) Self.Died();
        }

        public override void Heal(int amount)
        {
            if (!Alive || amount <= 0) return;

            hp = Math.Min(hp + amount, max);
        }

        public override void Resurrect()
        {
            if (Alive) return;

            hp = max;
        }
    }
}
