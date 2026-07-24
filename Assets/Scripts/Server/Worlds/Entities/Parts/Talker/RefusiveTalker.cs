namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public class RefusiveTalker : Talker
    {
        public RefusiveTalker(Health.Health health) : base(health)
        {
        }

        protected override void StartTalking(long userId)
        {
            Say(userId, "Не сейчас.");
        }
    }
}
