namespace Shooter.Game.Llm.Ticker.Children
{
    public class LlmConversationTicker : LlmChildTicker
    {
        public override bool TickRequired(LlmStatus llmStatus)
        {
            return llmStatus.PendingConversations;
        }

        public override void RegisterTick()
        {

        }
    }
}
