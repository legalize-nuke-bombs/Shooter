namespace Shooter.Game.Body
{
    public static class SleepRule
    {
        public static bool CanSleep(bool alive, bool handsFree, bool bedtime)
        {
            return alive && handsFree && bedtime;
        }

        public static bool CanWake(bool worldAsleep)
        {
            return !worldAsleep;
        }
    }
}
