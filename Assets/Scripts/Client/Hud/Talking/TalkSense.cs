using Shooter.Client.Aiming;
using Shooter.Client.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Talker;

namespace Shooter.Client.Hud.Talking
{
    public class TalkSense
    {
        private readonly Aim aim;

        public TalkSense(Aim aim)
        {
            this.aim = aim;
        }

        public EntityView TargetTalker()
        {
            EntityView view = aim.TargetView(Talker.TalkReach);
            if (view == null) return null;

            return view.Talkative && view.Alive ? view : null;
        }

        public bool TalkerTargeted()
        {
            return TargetTalker() != null;
        }
    }
}
