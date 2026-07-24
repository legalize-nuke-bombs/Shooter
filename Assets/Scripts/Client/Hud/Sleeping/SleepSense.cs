using Shooter.Client.Aiming;
using Shooter.Client.Worlds;
using Shooter.Client.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Hands;
using Shooter.Server.Worlds.Sleeping;
using Shooter.Server.Worlds.Time;

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
                EntityView me = world.Me;
                return me != null && me.Sleeping;
            }
        }

        public bool WorldAsleep => world.WorldAsleep;

        public bool CanSleep => !MySleeping && SleepRule.CanSleep(HandsFree, Night, LookingAtBed);

        private bool HandsFree
        {
            get
            {
                EntityView me = world.Me;
                return me == null || me.HandsAction == HandsAction.None;
            }
        }

        private bool Night => world.Clock != null && DayCycle.IsNight(DayCycle.FractionOf(world.Clock.Timestamp));

        private bool LookingAtBed => aim.Target != null
                                     && aim.Target.Value.distance <= Sleep.UseReach
                                     && Sleep.IsBed(aim.Target.Value);
    }
}
