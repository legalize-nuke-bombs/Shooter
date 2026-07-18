namespace Shooter.Server.Entities.Chronology
{
    public class Clock
    {
        private const float DayRealSeconds = 120f;

        public double Timestamp { get; private set; }

        public void Tick(float dt)
        {
            Timestamp += dt * (ClockState.DayLengthSeconds / DayRealSeconds);
        }

        public ClockState State()
        {
            return new ClockState { Timestamp = (long)Timestamp };
        }
    }
}
