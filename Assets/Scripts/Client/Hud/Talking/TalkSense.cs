using Shooter.Client.Aiming;
using Shooter.Client.Worlds;
using Shooter.Client.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Talker;

namespace Shooter.Client.Hud.Talking
{
    public class TalkSense
    {
        private readonly ClientWorld world;
        private readonly Aim aim;

        public TalkSense(ClientWorld world, Aim aim)
        {
            this.world = world;
            this.aim = aim;
        }

        public EntityView TargetTalker()
        {
            EntityView me = world.Me;
            EntityView target = aim.TargetView(Talker.TalkReach);
            if (target == null) return null;

            bool canTalk = TalkRule.CanTalk(me != null && me.Alive, target.Alive, target.Talkative);
            return canTalk ? target : null;
        }

        public bool TalkerTargeted()
        {
            return TargetTalker() != null;
        }
    }
}
