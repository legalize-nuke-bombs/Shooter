using System;

namespace Shooter.Server.Chronology
{
    [Serializable]
    public class ClockState
    {
        public const long DayLengthSeconds = 86400;

        public long timestamp;

        public ClockState() { }

        public ClockState(Clock clock)
        {
            timestamp = (long)clock.Timestamp;
        }

        public float Fraction()
        {
            return (timestamp % DayLengthSeconds) / (float)DayLengthSeconds;
        }

        public int Day()
        {
            return (int)(timestamp / DayLengthSeconds);
        }
    }
}
