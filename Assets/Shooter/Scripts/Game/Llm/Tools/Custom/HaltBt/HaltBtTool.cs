using System;
using Shooter.Infrastructure;
using Unity.Behavior;

namespace Shooter.Game
{
    [Serializable]
    public sealed class HaltBtTool : LlmTool<HaltBtArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private BehaviorGraphAgent btAgent;

        public override string Name => "halt_bt";

        public override string Description =>
            "Instantly stops the active second-level behavior tree action, if there is one.";

        protected override void Start()
        {
            btAgent = Self.GetComponent<BehaviorGraphAgent>();
            if (btAgent == null)
            {
                Log.Error($"Entity {Self.name} does not have BehaviourGraphAgent component required by tool {Name}");
            }
        }

        protected override string Execute(HaltBtArguments arguments, LlmCallContext context)
        {
            btAgent.GetVariable("CustomOrderCurrent", out BlackboardVariable<CustomOrder> customOrderCurrent);
            if (customOrderCurrent.Value == CustomOrder.None) return "There is no active second-level action to stop";

            string output = $"{customOrderCurrent.Value} was stopped";
            btAgent.SetVariableValue("CustomOrderCurrent", CustomOrder.None);
            return output;
        }
    }
}
