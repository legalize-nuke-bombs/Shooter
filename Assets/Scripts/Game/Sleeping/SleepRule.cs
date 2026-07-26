namespace Shooter.Game.Sleeping
{
    public static class SleepRule
    {
        public static bool CanSleep(bool alive, bool handsFree, bool night)
        {
            return alive && handsFree && night;
        }

        public static bool CanWake(bool worldAsleep)
        {
            return !worldAsleep;
        }
    }
}
