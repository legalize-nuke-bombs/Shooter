namespace Shooter.Game
{
    // What was picked up in the bag window: a stack or its split pile, a unique item, a craft cell, the craft output or an offer
    public sealed class DraggedItem
    {
        private DraggedItem()
        {
        }

        public StackableItemAsset StackableAsset { get; private set; }
        public int SplitIndex { get; private set; } = -1;
        public bool IsUnique { get; private set; }
        public int UniqueSlot { get; private set; } = Inventory.NoSlot;
        public bool IsEquipable { get; private set; }
        public bool IsFromHands { get; private set; }
        public int CraftCell { get; private set; } = -1;
        public bool IsCraftOutput { get; private set; }
        public int OfferIndex { get; private set; } = -1;

        public static DraggedItem Stack(StackableItemAsset asset, int splitIndex)
        {
            return new DraggedItem { StackableAsset = asset, SplitIndex = splitIndex };
        }

        public static DraggedItem Unique(int slot, bool equipable, bool fromHands)
        {
            return new DraggedItem { IsUnique = true, UniqueSlot = slot, IsEquipable = equipable, IsFromHands = fromHands };
        }

        public static DraggedItem FromCraftCell(int cell)
        {
            return new DraggedItem { CraftCell = cell };
        }

        public static DraggedItem CraftOutput()
        {
            return new DraggedItem { IsCraftOutput = true };
        }

        public static DraggedItem Offer(int index)
        {
            return new DraggedItem { OfferIndex = index };
        }
    }
}
