using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    // The bag window: the bag and the hands, with the craft grid, the give panel or the searched loot, and the split box
    // beside them; whatever is dragged in it, the window decides what dropping it there means
    public class InventoryOverlay : Overlay
    {
        private const string WindowElementName = "inventory-screen";
        private const string BagScrollElementName = "inventory-scroll";
        private const string BagGridElementName = "inventory-grid";
        private const string HandsGridElementName = "inventory-held";
        private const string CraftGridElementName = "craft-grid";
        private const string CraftOutputElementName = "craft-output";
        private const string GivePanelElementName = "give";
        private const string GiveTitleLabelName = "give-title";
        private const string GiveGridElementName = "give-grid";
        private const string GiveButtonName = "give-button";
        private const string LootPanelElementName = "loot";
        private const string LootTitleLabelName = "loot-title";
        private const string LootGridElementName = "loot-grid";
        private const string LootTakeAllButtonName = "loot-take-all";
        private const int BagGridColumns = 10;
        private const int BagGridRows = 6;
        private const int HandsGridRows = 2;
        private static readonly Journal Log = Logs.Here();

        private Inventory inventory;
        private InventoryHolder inventoryHolder;
        private InventoryUser inventoryUser;
        private VisualElement windowElement;
        private VisualElement bagScrollElement;
        private VisualElement bagGridElement;
        private VisualElement handsGridElement;
        private ItemDragging dragging;
        private ItemContextMenu contextMenu;
        private StackSplitPrompt splitPrompt;
        private CraftPanel craftPanel;
        private GivePanel givePanel;
        private LootPanel lootPanel;
        private bool open;
        private bool stale;

        private void Update()
        {
            if (!IsBound) return;

            PlayerController playerController = LocalPlayerFinder.FindComponent<PlayerController>();
            bool shouldBeOpen = playerController != null && playerController.IsInventoryOpen;

            if (shouldBeOpen != open)
            {
                open = shouldBeOpen;

                if (open)
                {
                    OpenWindow(playerController.Loot);
                }
                else
                {
                    CloseWindow();
                }
            }

            if (!open) return;

            if (playerController.Loot != lootPanel.Loot) SearchOtherLoot(playerController.Loot);

            if (lootPanel.IsOpen && !lootPanel.IsInReach)
            {
                Log.Info($"{lootPanel.Loot.name} is out of reach, the bag closes");
                playerController.CloseInventory();
                return;
            }

            if (splitPrompt.IsOpen && !playerController.IsPromptOpen) splitPrompt.Hide();

            givePanel.Update();
            if (stale) RedrawInventory();
        }

        protected override bool TryBindElements(VisualElement root)
        {
            windowElement = root.Q<VisualElement>(WindowElementName);
            bagScrollElement = root.Q<VisualElement>(BagScrollElementName);
            bagGridElement = root.Q<VisualElement>(BagGridElementName);
            handsGridElement = root.Q<VisualElement>(HandsGridElementName);
            VisualElement craftGridElement = root.Q<VisualElement>(CraftGridElementName);
            VisualElement craftOutputElement = root.Q<VisualElement>(CraftOutputElementName);
            VisualElement givePanelElement = root.Q<VisualElement>(GivePanelElementName);
            Label giveTitleLabel = root.Q<Label>(GiveTitleLabelName);
            VisualElement giveGridElement = root.Q<VisualElement>(GiveGridElementName);
            Button giveButton = root.Q<Button>(GiveButtonName);
            VisualElement lootPanelElement = root.Q<VisualElement>(LootPanelElementName);
            Label lootTitleLabel = root.Q<Label>(LootTitleLabelName);
            VisualElement lootGridElement = root.Q<VisualElement>(LootGridElementName);
            Button lootTakeAllButton = root.Q<Button>(LootTakeAllButtonName);

            if (windowElement == null || bagScrollElement == null || bagGridElement == null || handsGridElement == null || craftGridElement == null || craftOutputElement == null || givePanelElement == null || giveTitleLabel == null || giveGridElement == null || giveButton == null ||
                lootPanelElement == null || lootTitleLabel == null || lootGridElement == null || lootTakeAllButton == null)
            {
                Log.Error($"Overlay document has no {WindowElementName} window, the bag stays hidden");
                return false;
            }

            windowElement.style.display = DisplayStyle.None;
            dragging = new ItemDragging(windowElement, HandleDropped);
            contextMenu = new ItemContextMenu(windowElement);
            splitPrompt = new StackSplitPrompt(windowElement, MarkStale);
            craftPanel = new CraftPanel(craftGridElement, craftOutputElement, dragging, MarkStale);
            givePanel = new GivePanel(givePanelElement, giveTitleLabel, giveGridElement, giveButton, dragging, MarkStale);
            lootPanel = new LootPanel(lootPanelElement, lootTitleLabel, lootGridElement, lootTakeAllButton, dragging, MarkStale);

            return true;
        }

        protected override void UnbindElements()
        {
            if (open) CloseWindow();

            givePanel?.Unbind();
            lootPanel?.Unbind();

            open = false;
            windowElement = null;
        }

        private void OpenWindow(Lootable loot)
        {
            inventory = LocalPlayerFinder.FindComponent<Inventory>();
            inventoryHolder = LocalPlayerFinder.FindComponent<InventoryHolder>();
            inventoryUser = LocalPlayerFinder.FindComponent<InventoryUser>();
            craftPanel.Open(inventory, LocalPlayerFinder.FindComponent<Crafter>());
            OpenOtherSide(loot);

            if (inventory != null) inventory.Changed += MarkStale;
            if (inventoryHolder != null) inventoryHolder.Changed += MarkStale;

            windowElement.style.display = DisplayStyle.Flex;
            stale = true;

            string takerName = givePanel.DescribeTaker();
            Log.Info(loot != null ? $"The bag is open on {loot.name}" : takerName == null ? "The bag is open" : $"The bag is open, {takerName} can take things from it");
        }

        // The other side of the bag is the searched loot, or else whoever in view can take things
        private void OpenOtherSide(Lootable loot)
        {
            InventoryExchanger inventoryExchanger = LocalPlayerFinder.FindComponent<InventoryExchanger>();
            if (loot != null && inventoryExchanger != null)
            {
                lootPanel.Open(loot, inventoryExchanger);
            }
            else
            {
                givePanel.Open(inventory, inventoryExchanger, LocalPlayerFinder.FindComponent<Character>());
            }
        }

        private void SearchOtherLoot(Lootable loot)
        {
            lootPanel.Close();
            givePanel.Close();
            OpenOtherSide(loot);
            stale = true;
        }

        private void CloseWindow()
        {
            contextMenu.Close();
            splitPrompt.Close();

            if (inventory != null) inventory.Changed -= MarkStale;
            if (inventoryHolder != null) inventoryHolder.Changed -= MarkStale;
            inventory = null;
            inventoryHolder = null;
            inventoryUser = null;
            craftPanel.Close();
            givePanel.Close();
            lootPanel.Close();
            splitPrompt.Clear();

            if (windowElement != null) windowElement.style.display = DisplayStyle.None;
            bagGridElement?.Clear();
            handsGridElement?.Clear();
            Log.Info("The bag is closed");
        }

        private void MarkStale()
        {
            stale = true;
        }

        private void RedrawInventory()
        {
            stale = false;
            contextMenu.Close();
            ReconcileWithInventory();
            bagGridElement.Clear();
            handsGridElement.Clear();

            ItemGrid.DrawCells(handsGridElement, HandsGridRows, BagGridColumns);

            if (inventory == null)
            {
                ItemGrid.DrawCells(bagGridElement, BagGridRows, BagGridColumns);
                return;
            }

            ItemCatalog itemCatalog = Catalogs.GetCatalog<ItemCatalog>();
            UniqueItem heldItem = inventoryHolder.HeldItem;
            int heldSlot = inventoryHolder.HeldSlot;

            if (heldItem != null && !givePanel.IsSlotOffered(heldSlot))
            {
                ItemAsset asset = itemCatalog == null ? null : itemCatalog.GetAssetById(heldItem.AssetId);
                handsGridElement.Add(CreateUniqueElement(asset, heldItem.AssetId, 0, 0, heldSlot, true, true));
            }

            // The bag grows by rows when its things do not fit, and its window scrolls
            GridPlacement placement = new(BagGridRows, BagGridColumns, true);
            List<VisualElement> itemElements = new();

            IReadOnlyList<UniqueItem> uniqueItems = inventory.UniqueItems;
            for (int slot = 0; slot < uniqueItems.Count; slot++)
            {
                UniqueItem uniqueItem = uniqueItems[slot];
                if (uniqueItem == null || slot == heldSlot || givePanel.IsSlotOffered(slot)) continue;

                ItemAsset asset = itemCatalog == null ? null : itemCatalog.GetAssetById(uniqueItem.AssetId);
                placement.TryPlace(ItemGrid.GetSizeInCells(asset), out int row, out int column);
                bool equipable = asset is UniqueItemAsset uniqueItemAsset && uniqueItemAsset.IsEquipable;
                itemElements.Add(CreateUniqueElement(asset, uniqueItem.AssetId, row, column, slot, equipable, false));
            }

            int assetCount = itemCatalog == null ? 0 : itemCatalog.Count;
            for (int index = 0; index < assetCount; index++)
            {
                if (itemCatalog.GetAssetAt(index) is not StackableItemAsset asset) continue;

                int amount = CountFree(asset);
                if (amount > 0)
                {
                    placement.TryPlace(ItemGrid.GetSizeInCells(asset), out int row, out int column);
                    itemElements.Add(CreateStackElement(asset, row, column, amount, -1));
                }

                for (int pileIndex = 0; pileIndex < splitPrompt.Piles.Count; pileIndex++)
                {
                    ItemStack pile = splitPrompt.Piles[pileIndex];
                    if (pile.Asset != asset) continue;

                    placement.TryPlace(ItemGrid.GetSizeInCells(asset), out int row, out int column);
                    itemElements.Add(CreateStackElement(asset, row, column, pile.Count, pileIndex));
                }
            }

            ItemGrid.DrawCells(bagGridElement, placement.Rows, BagGridColumns);
            foreach (VisualElement itemElement in itemElements)
            {
                bagGridElement.Add(itemElement);
            }

            craftPanel.Draw();
            givePanel.Draw();
            lootPanel.Draw();
        }

        // A result that reached the bag pays one unit from every craft cell; an offer the bag let go is done; what
        // the bag no longer backs leaves the split piles first, then the offer, then the craft grid
        private void ReconcileWithInventory()
        {
            if (inventory == null)
            {
                craftPanel.Close();
                givePanel.Close();
                splitPrompt.Clear();
                return;
            }

            craftPanel.PayForArrivedOutput();
            if (givePanel.IsOpen) givePanel.ForgetGiven();
            splitPrompt.ReleaseUnbacked(inventory, CountReserved);
            if (givePanel.IsOpen) givePanel.ReleaseUnbacked(CountReserved);
            craftPanel.ReleaseUnbacked(CountReserved);
        }

        private int CountReserved(StackableItemAsset asset)
        {
            return craftPanel.CountOf(asset) + splitPrompt.CountOf(asset) + givePanel.CountOf(asset);
        }

        private int CountFree(StackableItemAsset asset)
        {
            return inventory == null ? 0 : inventory.CountStackable(asset) - CountReserved(asset);
        }

        private VisualElement CreateUniqueElement(ItemAsset asset, string fallbackTitle, int row, int column, int slot, bool equipable, bool inHands)
        {
            Vector2 size = ItemGrid.GetSize(asset);
            VisualElement itemElement = ItemGrid.CreateSlotElement(asset, fallbackTitle, inHands, equipable);
            ItemGrid.PlaceAt(itemElement, row, column, size);
            dragging.MakeDraggable(itemElement, asset, size, DraggedItem.Unique(slot, equipable, inHands));

            return itemElement;
        }

        private VisualElement CreateStackElement(StackableItemAsset asset, int row, int column, int amount, int pileIndex)
        {
            Vector2 size = ItemGrid.GetSize(asset);
            VisualElement itemElement = ItemGrid.CreateSlotElement(asset, asset.Id, false, false);
            ItemGrid.PlaceAt(itemElement, row, column, size);
            ItemGrid.AddAmountLabel(itemElement, amount);
            dragging.MakeDraggable(itemElement, asset, size, DraggedItem.Stack(asset, pileIndex));
            if (pileIndex < 0) AddContextMenu(itemElement, asset, amount);

            return itemElement;
        }

        private void AddContextMenu(VisualElement itemElement, StackableItemAsset asset, int amount)
        {
            itemElement.RegisterCallback<PointerDownEvent>(pointerDown =>
            {
                if (pointerDown.button != 1 || dragging.IsDragging) return;

                List<(string Text, Action Action)> items = new();
                if (asset.IsUsable) items.Add(("Использовать", () => UseItem(asset)));
                if (amount > 1) items.Add(("Разделить", () => splitPrompt.Open(asset, () => CountFree(asset))));

                contextMenu.Open(pointerDown.position, items);
                pointerDown.StopPropagation();
            });
        }

        private void UseItem(StackableItemAsset asset)
        {
            if (inventoryUser != null) inventoryUser.UseItemRpc(asset.NetworkId);
        }

        private void HandleDropped(DraggedItem item, Vector2 position)
        {
            if (inventory == null) return;

            bool isOverBag = bagScrollElement.worldBound.Contains(position) || handsGridElement.worldBound.Contains(position);
            bool isOverGivePanel = givePanel.IsOver(position);
            bool isOverLootPanel = lootPanel.IsOver(position);
            int craftCell = craftPanel.FindCellAt(position);

            if (item.IsFromLoot)
            {
                if (isOverBag) lootPanel.Take(item);
                return;
            }

            if (item.IsCraftOutput)
            {
                if (isOverBag) craftPanel.Craft();
                return;
            }

            if (item.OfferIndex >= 0)
            {
                if (!isOverGivePanel) givePanel.RemoveOffer(item.OfferIndex);
                stale = true;
                return;
            }

            if (item.CraftCell >= 0)
            {
                ItemStack movedStack = craftPanel.GetStackAt(item.CraftCell);

                if (isOverGivePanel)
                {
                    if (movedStack.Asset != null && givePanel.TryOfferStackable(movedStack.Asset, movedStack.Count)) craftPanel.Empty(item.CraftCell);
                }
                else if (craftCell < 0)
                {
                    craftPanel.Empty(item.CraftCell);
                }
                else
                {
                    craftPanel.Move(item.CraftCell, craftCell);
                }

                stale = true;
                return;
            }

            if (item.StackableAsset != null)
            {
                bool isPile = item.SplitIndex >= 0 && item.SplitIndex < splitPrompt.Piles.Count;
                int amount = isPile ? splitPrompt.Piles[item.SplitIndex].Count : CountFree(item.StackableAsset);
                if (amount <= 0) return;

                if (isOverLootPanel) lootPanel.PutStackable(item.StackableAsset, amount);

                bool isMoved = isOverLootPanel || (isOverGivePanel
                    ? givePanel.TryOfferStackable(item.StackableAsset, amount)
                    : craftCell >= 0 && craftPanel.TryPut(craftCell, item.StackableAsset, amount));
                if (!isMoved) return;

                if (isPile) splitPrompt.RemovePileAt(item.SplitIndex);
                stale = true;
                return;
            }

            if (!item.IsUnique) return;

            if (isOverLootPanel)
            {
                lootPanel.PutUnique(item.UniqueSlot);
                return;
            }

            if (isOverGivePanel)
            {
                if (givePanel.TryOfferUnique(item.UniqueSlot)) stale = true;
                return;
            }

            if (!item.IsEquipable) return;

            if (handsGridElement.worldBound.Contains(position) && !item.IsFromHands)
            {
                inventoryHolder.HoldSlotRpc(item.UniqueSlot);
            }
            else if (bagScrollElement.worldBound.Contains(position) && item.IsFromHands)
            {
                inventoryHolder.HoldSlotRpc(Inventory.NoSlot);
            }
        }
    }
}
