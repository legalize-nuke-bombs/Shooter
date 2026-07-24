using System;


namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public class RefusiveTalker : Talker
    {
        public RefusiveTalker(Guid selfId, ServerWorld world) : base(selfId, world)
        {
        }

        protected override void StartTalking(long userId)
        {
            Say(userId, "Not now.");
        }
    }
}
