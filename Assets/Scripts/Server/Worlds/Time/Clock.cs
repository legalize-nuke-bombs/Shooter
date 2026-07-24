namespace Shooter.Server.Worlds.Time
{
    public class Clock
    {
        public const float DayRealSeconds = 600f;

        public double Timestamp { get; private set; }

        public void Tick(float dt)
        {
            Timestamp += dt * (DayCycle.DayLengthSeconds / DayRealSeconds);
        }

        public bool IsNight()
        {
            return DayCycle.IsNight(DayCycle.FractionOf((long)Timestamp));
        }

        public ClockState State()
        {
            return new ClockState { Timestamp = (long)Timestamp };
        }
    }
}
