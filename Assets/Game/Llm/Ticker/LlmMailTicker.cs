using System;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [Serializable]
    public class LlmMailTicker : LlmChildTicker
    {
        [SerializeField] private float pendingInterval = 2.5f;
        private float? pendingSince;

        public override void RegisterTick()
        {
        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            if (llmStatus.PendingMail)
            {
                float now = Time.time;
                if (pendingSince == null) pendingSince = now;
                return now - pendingSince.Value >= pendingInterval;
            }

            pendingSince = null;
            return false;
        }
    }
}
