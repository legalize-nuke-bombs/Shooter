namespace Shooter.Server.Worlds.Entities.Parts.Health
{
    public abstract class Health : Part
    {
        protected Health(Entity self) : base(self, typeof(Health))
        {
        }

        public abstract int Hp { get; }
        public abstract int MaxHp { get; }
        public abstract bool Alive { get; }
        public abstract void Damage(int amount);
        public abstract void Heal(int amount);
        public abstract void Resurrect();

        public override string Digest()
        {
            return Alive ? $"Здоровье: {Hp}/{MaxHp}" : "Мертв";
        }

        public override PartState State()
        {
            return new HealthState { Hp = Hp, MaxHp = MaxHp, Alive = Alive };
        }
    }
}
