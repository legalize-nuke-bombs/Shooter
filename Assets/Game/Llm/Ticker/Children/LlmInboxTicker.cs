namespace Shooter.Game.Llm.Ticker.Children
{
    public class LlmInboxTicker : LlmChildTicker
    {
        public override void RegisterTick()
        {

        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            return llmStatus.PendingInterNpcInteractionsInbox || llmStatus.PendingSystemNotificationsInbox;
        }
    }
}
