namespace Shooter.Game.Body
{
    public class UnlimitedHunger : Hunger
    {
        public override float Amount => 100f;
        public override float MaxAmount => 100f;


        public override bool CanSpend(float a)
        {
            return true;
        }

        public override void Spend(float a)
        {
        }

        public override void Restore(float a)
        {
        }
    }
}
