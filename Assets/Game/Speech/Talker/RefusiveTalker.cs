namespace Shooter.Game.Speech
{
    public sealed class RefusiveTalker : Talker
    {
        protected override bool Busy()
        {
            return false;
        }

        protected override void RequestAnswer(long wandererId, string message, bool spoken)
        {
            ConversationManager.Current.Say(CharacterId, wandererId, "Not now.", false);
        }
    }
}
