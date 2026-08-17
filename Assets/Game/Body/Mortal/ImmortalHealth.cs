namespace Shooter.Game.Body
{
    public sealed class ImmortalHealth : Health
    {
        public override double Hp => 100;

        public override double MaxHp => 100;

        public override bool Alive => true;

        protected override void DamageRaw(double amount)
        {
        }

        public override void Heal(double amount)
        {
        }

        public override void Resurrect()
        {
        }
    }
}
