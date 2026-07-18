namespace Shooter.Server.Worlds.Entities.Chronology
{
    public class ClockState
    {
        public const long DayLengthSeconds = 86400;
        public const float DawnFraction = 0.25f;
        public const float DuskFraction = 0.75f;

        public long Timestamp { get; set; }

        public static float FractionOf(long timestamp)
        {
            return (timestamp % DayLengthSeconds) / (float)DayLengthSeconds;
        }

        public static bool IsNight(float fraction)
        {
            return fraction >= DuskFraction || fraction < DawnFraction;
        }

        public float Fraction()
        {
            return FractionOf(Timestamp);
        }

        public int Day()
        {
            return (int)(Timestamp / DayLengthSeconds);
        }
    }
}
