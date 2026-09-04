using System;
using Shooter.Game.AI.Bt.CustomOrders;
using Shooter.Logging;

namespace Shooter.Game.Llm.HaltBt
{
    [Serializable]
    public sealed class HaltBtTool : LlmTool<HaltBtArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private BtCustomOrderQueue customOrders;

        public override string Name => "halt_bt";

        public override string Description =>
            "Instantly stops the active second-level behavior tree action, if there is one.";

        protected override void OnStart()
        {
            customOrders = Self.GetComponent<BtCustomOrderQueue>();
            if (customOrders == null)
            {
                Log.Error($"Entity {Self.name} does not have BtCustomOrderQueue component required by tool {Name}");
            }
        }

        protected override string Execute(HaltBtArguments arguments, LlmCallContext context)
        {
            BtCustomOrder current = customOrders.Current;
            if (current == null) return "There is no active second-level action to stop";

            string stopped = current.PromptDescription(Self);
            customOrders.Clear();
            return $"Stopped: {stopped}";
        }
    }
}
