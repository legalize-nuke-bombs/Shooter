namespace Shooter.Game.Restraining
{
    public static class Restraints
    {
        public static bool Any(IRestraint[] restraints)
        {
            foreach (IRestraint restraint in restraints)
            {
                if (restraint.Restrains) return true;
            }

            return false;
        }
    }
}
