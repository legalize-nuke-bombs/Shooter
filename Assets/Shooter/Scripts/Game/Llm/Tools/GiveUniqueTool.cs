using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public sealed class GiveUniqueTool : LlmTool<GiveUniqueArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private InventoryExchanger exchanger;

        public override string Name => "give_unique";

        public override string Description =>
            @$"
Give one of your unique items, by its slot number, to a character within {exchanger.Radius} meters.
The recipient will automatically receive a notification.
";

        protected override void OnStart()
        {
            exchanger = Self.GetComponent<InventoryExchanger>();
            if (exchanger == null)
            {
                Log.Error($"Entity {Self.name} does not have exchanger component required by tool {Name}");
            }
        }

        protected override string Execute(GiveUniqueArguments arguments, LlmCallContext context)
        {
            return exchanger.Give(Character.Of(arguments.TargetId, Inactive.Exclude), arguments.Slot)
                ? $"Gave the item from slot {arguments.Slot} to {arguments.TargetId}"
                : "Could not give: the receiver is not around or the slot is empty";
        }
    }
}
