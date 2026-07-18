using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Server.Worlds;

namespace Shooter.Server.Worlds.Entities.Sleeping
{
    public class Sleep
    {
        public const float UseReach = 4f;

        private const string BedName = "Bed";

        private const float SkipTimeScale = 6f;

        private readonly Clock clock;
        private readonly ServerWorldPlayers players;
        private bool wasNight;

        public static bool IsBed(string objectName)
        {
            return objectName.Contains(BedName, System.StringComparison.OrdinalIgnoreCase);
        }

        public Sleep(Clock clock, ServerWorldPlayers players)
        {
            this.clock = clock;
            this.players = players;
        }

        public bool WorldAsleep()
        {
            return players.AllAsleep();
        }

        public SleepState State()
        {
            return new SleepState { WorldAsleep = WorldAsleep() };
        }

        public float ClockScale()
        {
            return WorldAsleep() ? SkipTimeScale : 1f;
        }

        public void Tick()
        {
            if (clock.IsNight())
            {
                wasNight = true;
                return;
            }
            if (!wasNight) return;
            wasNight = false;
            Log.Info("Dawn broke, waking sleepers");
            players.WakeAll();
        }
    }
}
