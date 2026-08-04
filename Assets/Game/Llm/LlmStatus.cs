namespace Shooter.Game.Llm
{
    public class LlmStatus
    {
        public bool PendingConversations { get; set; }
        public bool PendingInterNpcInteractionsInbox { get; set; }
        public bool PendingSystemNotificationsInbox { get; set; }
    }
}
