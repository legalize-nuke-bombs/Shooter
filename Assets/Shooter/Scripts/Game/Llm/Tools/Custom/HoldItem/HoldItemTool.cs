using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class HoldItemTool : LlmTool<HoldItemArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private InventoryHolder inventoryHolder;

        public override string Name => "hold_item";

        public override string Description => @"
Hold equipable unique item (firearm, etc.) from your inventory by its slot number.
You can hold only one equipable unique item at a time.
If an item is held in the hand, invoking this tool will replace it with the provided one.
Pass -1 as slot number to hold nothing.
Always prefer to keep a weapon in your hand.
";


        protected override void Start()
        {
            inventoryHolder = Self.GetComponent<InventoryHolder>();
            if (inventoryHolder == null)
            {
                Log.Error($"Entity {Self.name} does not have inventory holder component required by tool {Name}");
            }
        }

        protected override string Execute(HoldItemArguments arguments, LlmCallContext context)
        {
            int slotId = arguments.Slot;
            if (slotId < -1)
            {
                return $"Bad slot number ({slotId})";
            }
            InventoryHolder.TryHoldSlotResultCode result = inventoryHolder.HoldSlot(slotId);
            switch (result)
            {
                case InventoryHolder.TryHoldSlotResultCode.SlotIsEmpty:
                    return $"Slot {slotId} is empty";
                case InventoryHolder.TryHoldSlotResultCode.SlotIsNotEquipable:
                    return $"Slot {slotId} is not equipable";
                case InventoryHolder.TryHoldSlotResultCode.Ok:
                    return $"Slot {slotId} held successfully";
                default:
                    Log.Error($"Entity {Self.name} got unexpected result code {result} during inventory holder try hold");
                    return $"Unexpected result code {result}";
            }
        }
    }
}
