namespace Shooter.Server.Entities.Chronology
{
    public class ClockState
    {
        public const long DayLengthSeconds = 86400;

        public long Timestamp { get; set; }

        public float Fraction()
        {
            return (Timestamp % DayLengthSeconds) / (float)DayLengthSeconds;
        }

        public int Day()
        {
            return (int)(Timestamp / DayLengthSeconds);
        }
    }
}
