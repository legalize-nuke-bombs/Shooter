namespace Shooter.Game
{
    // What was picked up in the bag window: a stack or its split pile, a unique item, a craft cell, the craft output, an
    // offer, or a stack or a unique item of the searched loot
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
        public bool IsFromBelt { get; private set; }
        public int CraftCell { get; private set; } = -1;
        public bool IsCraftOutput { get; private set; }
        public int OfferIndex { get; private set; } = -1;
        public StackableItemAsset LootStackableAsset { get; private set; }
        public int LootSlot { get; private set; } = Inventory.NoSlot;

        public bool IsFromLoot => LootStackableAsset != null || LootSlot != Inventory.NoSlot;

        public static DraggedItem Stack(StackableItemAsset asset, int splitIndex)
        {
            return new DraggedItem { StackableAsset = asset, SplitIndex = splitIndex };
        }

        public static DraggedItem Unique(int slot, bool equipable, bool fromHands, bool fromBelt)
        {
            return new DraggedItem { IsUnique = true, UniqueSlot = slot, IsEquipable = equipable, IsFromHands = fromHands, IsFromBelt = fromBelt };
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

        public static DraggedItem LootStack(StackableItemAsset asset)
        {
            return new DraggedItem { LootStackableAsset = asset };
        }

        public static DraggedItem LootUnique(int slot)
        {
            return new DraggedItem { LootSlot = slot };
        }
    }
}
