using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(LlmPendingTable))]
    public class LlmTalkTicker : LlmChildTicker
    {
        private LlmPendingTable table;

        private void Awake()
        {
            table = GetComponent<LlmPendingTable>();
        }

        public override void RegisterTick()
        {
        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            return table.Any;
        }
    }
}
