namespace Shooter.Game.Body
{
    public sealed class ImmortalHealth : Health
    {
        public override int Hp => 100;

        public override int MaxHp => 100;

        public override bool Alive => true;

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
