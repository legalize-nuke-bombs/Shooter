namespace Shooter.Game.Body
{
    public sealed class DeadHealth : Health
    {
        public override double Hp => 0;

        public override double MaxHp => 100;

        public override bool Alive => false;

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
