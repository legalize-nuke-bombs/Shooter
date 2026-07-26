namespace Shooter.Game.Vitals
{
    public sealed class DeadHealth : Health
    {
        public override int Hp => 0;

        public override int MaxHp => 1;

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
