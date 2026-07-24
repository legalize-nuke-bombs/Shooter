using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Sleeper;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Sleeping
{
    public sealed class Sleep
    {
        public const float UseReach = 4f;

        private const string BedName = "Bed";
        private const float SkipTimeScale = 6f;

        private readonly Clock clock;
        private readonly WorldEntities entities;

        private bool wasNight;

        public Sleep(Clock clock, WorldEntities entities)
        {
            this.clock = clock;
            this.entities = entities;
        }

        public bool WorldAsleep { get; private set; }

        public static bool IsBed(RaycastHit hit)
        {
            return hit.collider.name.StartsWith(BedName, System.StringComparison.OrdinalIgnoreCase);
        }

        public void Tick(float dt)
        {
            WorldAsleep = AllAsleep();
            clock.Tick(dt * (WorldAsleep ? SkipTimeScale : 1f));

            if (clock.IsNight())
            {
                wasNight = true;
                return;
            }

            if (!wasNight) return;
            wasNight = false;
            Log.Info("Dawn broke, waking sleepers");
            WakeAll();
        }

        public SleepState State()
        {
            return new SleepState { WorldAsleep = WorldAsleep };
        }

        private bool AllAsleep()
        {
            bool anyone = false;
            foreach (Entity player in entities.Players())
            {
                Sleeper sleeper = player.Get<Sleeper>();
                if (sleeper == null || !sleeper.Sleeping) return false;
                anyone = true;
            }
            return anyone;
        }

        private void WakeAll()
        {
            foreach (Entity player in entities.Players())
                player.Get<Sleeper>()?.WakeUp();
        }
    }
}
