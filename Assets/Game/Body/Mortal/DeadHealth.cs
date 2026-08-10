namespace Shooter.Game.Body
{
    public sealed class DeadHealth : Health
    {
        public override int Hp => 0;

        public override int MaxHp => 100;

        public override bool Alive => false;

        protected override void DamageRaw(int amount)
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
