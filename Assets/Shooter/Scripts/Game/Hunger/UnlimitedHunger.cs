namespace Shooter.Game
{
    public class UnlimitedHunger : Hunger
    {
        public override float Amount => 100f;
        public override float MaxAmount => 100f;

        public override bool CanSpendAmount(float cost)
        {
            return true;
        }

        public override void SpendAmount(float cost)
        {
        }

        public override void RestoreAmount(float restoredAmount)
        {
        }
    }
}
