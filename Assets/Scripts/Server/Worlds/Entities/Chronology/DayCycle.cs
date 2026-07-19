namespace Shooter.Server.Worlds.Entities.Chronology
{
    public static class DayCycle
    {
        public const long DayLengthSeconds = 86400;
        public const float DawnFraction = 0.25f;
        public const float DuskFraction = 0.75f;

        public static float FractionOf(long timestamp)
        {
            return (timestamp % DayLengthSeconds) / (float)DayLengthSeconds;
        }

        public static bool IsNight(float fraction)
        {
            return fraction >= DuskFraction || fraction < DawnFraction;
        }

        public static int DayOf(long timestamp)
        {
            return (int)(timestamp / DayLengthSeconds);
        }
    }
}
