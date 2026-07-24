using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Protocol;
using Shooter.Server.Worlds.Sleeping;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Sleeper
{
    public sealed class Sleeper : Part
    {
        public bool Sleeping { get; private set; }

        public Vector3 SpawnPoint { get; private set; }

        private readonly Clock clock;
        private readonly Gaze gaze;

        public Sleeper(Entity self, Clock clock, Gaze gaze) : base(self, typeof(Sleeper))
        {
            this.clock = clock;
            this.gaze = gaze;
        }

        public override void Apply(PlayerIntent input)
        {
            Health.Health health = Self.Get<Health.Health>();
            if (health != null && !health.Alive) return;

            if (Sleeping)
            {
                if (input.Use || input.Jump) WakeUp();
                return;
            }

            if (input.Use) TryToSleep(input.Pitch, input.Yaw);
        }

        public bool TryToSleep(float pitch, float yaw)
        {
            Hands.Hands hands = Self.Get<Hands.Hands>();
            bool handsFree = hands == null || hands.Free;
            bool lookingAtBed = gaze.TryLook(Self.Position, pitch, yaw, Sleep.UseReach, out RaycastHit hit) && Sleep.IsBed(hit);

            if (!SleepRule.CanSleep(handsFree, clock.IsNight(), lookingAtBed))
            {
                Log.Info("Entity {} tried to sleep with hands free {}, night {}, bed in sight {}, ignored", Self.Name, handsFree, clock.IsNight(), lookingAtBed);
                return false;
            }

            Sleeping = true;
            SpawnPoint = Self.Position;
            Log.Info("Entity {} fell asleep at {}", Self.Name, SpawnPoint);
            return true;
        }

        public void WakeUp()
        {
            if (!Sleeping) return;

            Sleeping = false;
            Log.Info("Entity {} woke up at {}", Self.Name, Self.Position);
        }

        public override void Died()
        {
            WakeUp();
        }

        public override string Digest()
        {
            return Sleeping ? "Спит" : null;
        }

        public override PartState State()
        {
            return new SleeperState { Sleeping = Sleeping };
        }
    }
}
