namespace Shooter.Server.Worlds.Entities.Parts.Health
{
    public sealed class ImmortalHealth : Health
    {
        public override int Hp => 1;
        public override int MaxHp => 1;
        public override bool Alive => true;

        public override void Damage(int amount)
        {
        }

        public override void Heal(int amount)
        {
        }
    }
}
