using System;

namespace Shooter.Server.Worlds.Entities.Parts.Health
{
    public abstract class Health : Part
    {
        public sealed override Type Slot => typeof(Health);

        public abstract int Hp { get; }
        public abstract int MaxHp { get; }
        public abstract bool Alive { get; }
        public abstract void Damage(int amount);
        public abstract void Heal(int amount);

        public override PartState State()
        {
            return new HealthState { Hp = Hp, MaxHp = MaxHp };
        }
    }
}
