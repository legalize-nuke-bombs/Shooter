namespace Shooter.Game
{
    public sealed class DeadHealth : Health
    {
        public override double Hp => 0;

        public override double MaxHp => 100;

        public override bool IsAlive => false;

        protected override void ReduceHp(double amount)
        {
        }

        public override void RestoreHp(double amount)
        {
        }

        public override void ResurrectWithFullHp()
        {
        }
    }
}
