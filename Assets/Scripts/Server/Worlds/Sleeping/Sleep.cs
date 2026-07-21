using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Sleeping
{
    public class Sleep
    {
        public const float UseReach = 4f;

        private const string BedName = "Bed";

        private const float SkipTimeScale = 6f;

        private readonly Clock clock;
        private readonly Worlds.WorldEntities entities;
        private bool wasNight;

        public static bool IsBed(RaycastHit hit)
        {
            return hit.collider.name.Contains(BedName, System.StringComparison.OrdinalIgnoreCase);
        }

        public Sleep(Clock clock, Worlds.WorldEntities entities)
        {
            this.clock = clock;
            this.entities = entities;
        }

        public bool WorldAsleep()
        {
            return entities.AllAsleep();
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
            entities.WakeAll();
        }
    }
}
