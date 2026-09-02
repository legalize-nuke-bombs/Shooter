using Shooter.Game.AI.Bt.CustomOrders;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(BtCustomOrderQueue))]
    public sealed class HaltBtTool : LlmTool<HaltBtArguments>
    {
        private BtCustomOrderQueue customOrders;

        public override string Name => "halt_bt";

        public override string Description =>
            "Instantly stops the active second-level behavior tree action, if there is one.";

        protected override void Awake()
        {
            base.Awake();
            customOrders = GetComponent<BtCustomOrderQueue>();
        }

        protected override string Execute(HaltBtArguments arguments, LlmCallContext context)
        {
            BtCustomOrder current = customOrders.Current;
            if (current == null) return "There is no active second-level action to stop";

            string stopped = current.PromptDescription(gameObject);
            customOrders.Clear();
            return $"Stopped: {stopped}";
        }
    }
}
