using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(LlmWaiting))]
    public class LlmTalkTicker : LlmChildTicker
    {
        private LlmWaiting waiting;

        private void Awake()
        {
            waiting = GetComponent<LlmWaiting>();
        }

        public override void RegisterTick()
        {
        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            return waiting.Any;
        }
    }
}
