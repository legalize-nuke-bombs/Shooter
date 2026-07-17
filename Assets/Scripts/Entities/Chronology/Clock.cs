namespace Shooter.Entities.Chronology
{
    public class Clock
    {
        public const long DayLengthMs = 120000;

        private double timestampMs;

        public void Advance(float dt)
        {
            timestampMs += dt * 1000.0;
        }

        public ClockState BuildState()
        {
            return new ClockState { timestamp = (long)timestampMs };
        }

        public static float Fraction(long timestamp)
        {
            return (timestamp % DayLengthMs) / (float)DayLengthMs;
        }

        public static int Day(long timestamp)
        {
            return (int)(timestamp / DayLengthMs);
        }
    }
}
