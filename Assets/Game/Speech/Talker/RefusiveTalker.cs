namespace Shooter.Game.Speech
{
    public sealed class RefusiveTalker : Talker
    {
        protected override void RequestAnswer(long wandererId, string message)
        {
            DeliverAnswer(wandererId, null);
        }
    }
}
