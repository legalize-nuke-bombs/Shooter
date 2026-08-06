using UnityEngine;

namespace Shooter.Game.Llm.Ticker.Children
{
    public class LlmInboxTicker : LlmChildTicker
    {
        [SerializeField] private float pendingInterval = 2.5f;
        private float? pendingSince = null;

        public override void RegisterTick()
        {

        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            bool pending = llmStatus.PendingInterNpcInteractionsInbox || llmStatus.PendingSystemNotificationsInbox;

            if (pending)
            {
                float now = Time.time;
                if (pendingSince == null)
                {
                    pendingSince = now;
                }
                return (now - pendingSince.Value >= pendingInterval);
            }

            pendingSince = null;
            return false;
        }
    }
}
