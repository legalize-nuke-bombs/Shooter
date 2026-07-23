namespace Shooter.Server.Worlds.Entities.Parts.Health
{
    public sealed class DeadHealth : Health
    {
        public override int Hp => 0;
        public override int MaxHp => 0;
        public override bool Alive => false;

        public override void Damage(int amount)
        {
        }

        public override void Heal(int amount)
        {
        }

        public override void Resurrect()
        {
        }
    }
}
