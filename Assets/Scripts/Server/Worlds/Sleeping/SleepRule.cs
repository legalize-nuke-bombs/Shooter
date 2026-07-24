namespace Shooter.Server.Worlds.Sleeping
{
    public static class SleepRule
    {
        public static bool CanSleep(bool handsFree, bool night, bool lookingAtBed)
        {
            return handsFree && night && lookingAtBed;
        }
    }
}
