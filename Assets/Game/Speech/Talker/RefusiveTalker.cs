namespace Shooter.Game.Speech
{
    public sealed class RefusiveTalker : Talker
    {
        protected override bool Busy()
        {
            return false;
        }

        protected override void RequestAnswer(long wandererId, string message)
        {
            DeliverAnswer(wandererId, new Answer()
            {
                Content = "Not now.",
                Loud = false
            });
        }
    }
}
