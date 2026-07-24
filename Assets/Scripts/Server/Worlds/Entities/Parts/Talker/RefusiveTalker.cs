using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public class RefusiveTalker : Talker
    {
        public RefusiveTalker(Health.Health health, Clock clock) : base(health, clock)
        {
        }

        protected override void StartTalking(long userId)
        {
            Say(userId, "Not now.");
        }
    }
}
