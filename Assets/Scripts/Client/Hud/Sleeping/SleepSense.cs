using Shooter.Client.Aiming;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Time;
using Shooter.Server.Worlds.Entities.Parts;
using Shooter.Server.Worlds.Sleeping;

namespace Shooter.Client.Hud.Sleeping
{
    public class SleepSense
    {
        private readonly ClientWorld world;
        private readonly Aim aim;

        public SleepSense(ClientWorld world, Aim aim)
        {
            this.world = world;
            this.aim = aim;
        }

        public bool MySleeping
        {
            get
            {
                EntityState me = world.Me;
                PilotState pilot = me?.Part<PilotState>();
                return pilot != null && pilot.Sleeping;
            }
        }

        public bool WorldAsleep => world.Sleep != null && world.Sleep.WorldAsleep;

        public bool CanSleep => !MySleeping && Night
                                && aim.Target != null
                                && aim.Target.Value.distance <= Sleep.UseReach
                                && Sleep.IsBed(aim.Target.Value.collider.name);

        private bool Night => world.Clock != null && DayCycle.IsNight(DayCycle.FractionOf(world.Clock.Timestamp));
    }
}
