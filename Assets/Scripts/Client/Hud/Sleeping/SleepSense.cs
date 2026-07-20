using Shooter.Client.Aiming;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Server.Worlds.Entities.Players;
using Shooter.Server.Worlds.Entities.Sleeping;

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
                PlayerState me = world.Me;
                return me != null && me.Sleeping;
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
