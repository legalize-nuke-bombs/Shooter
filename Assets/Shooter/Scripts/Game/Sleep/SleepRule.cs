namespace Shooter.Game
{
    public static class SleepRule
    {
        public static bool CanWake(bool worldAsleep)
        {
            return !worldAsleep;
        }
    }
}
