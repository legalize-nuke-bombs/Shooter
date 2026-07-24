using System;

namespace Shooter.Server.Worlds.Time
{
    public class Clock
    {
        private const float DayRealSeconds = 120f;

        public double Timestamp { get; private set; }

        public void Tick(float dt)
        {
            Timestamp += dt * (DayCycle.DayLengthSeconds / DayRealSeconds);
        }

        public bool IsNight()
        {
            return DayCycle.IsNight(DayCycle.FractionOf((long)Timestamp));
        }

        public string DateTime()
        {
            const long delta2026 = 1767225600L;
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(delta2026 + (long)Timestamp);
            return dateTime.ToString("yyyy.MM.dd HH:mm:ss");
        }

        public ClockState State()
        {
            return new ClockState { Timestamp = (long)Timestamp };
        }
    }
}
