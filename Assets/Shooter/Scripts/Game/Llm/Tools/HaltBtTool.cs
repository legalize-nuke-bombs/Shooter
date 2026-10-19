using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public sealed class HaltBtTool : LlmTool<HaltBtArguments>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private CustomOrderSlot customOrderSlot;

        public override string Name => "halt_bt";

        public override string Description =>
            "Instantly stops the active second-level behavior tree action, if there is one.";

        protected override void OnStart()
        {
            customOrderSlot = Self.GetComponent<CustomOrderSlot>();
            if (customOrderSlot == null)
            {
                Log.Error($"Entity {Self.name} does not have CustomOrderSlot component required by tool {Name}");
            }
        }

        protected override string Execute(HaltBtArguments arguments, LlmCallContext context)
        {
            CustomOrder current = customOrderSlot.CurrentOrder;
            if (current == null) return "There is no active second-level action to stop";

            string stopped = current.GetPromptDescription(Self);
            customOrderSlot.ClearOrder();
            return $"Stopped: {stopped}";
        }
    }
}
