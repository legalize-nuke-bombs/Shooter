using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class InventoryOverlay : Overlay, IShooterUpdateable
    {
        private const string WindowElementName = "inventory-screen";
        private const string BagScrollElementName = "inventory-scroll";
        private const string BagGridElementName = "inventory-grid";
        private const string HandsGridElementName = "inventory-held";
        private const string ArtifactsGridElementName = "inventory-artifacts";
        private const string ArtifactsSectionElementName = "inventory-artifacts-section";
        private const string TooltipElementName = "item-tooltip";
        private const string TooltipTitleLabelName = "item-tooltip-title";
        private const string TooltipDescriptionLabelName = "item-tooltip-description";
        private const string TooltipStatsElementName = "item-tooltip-stats";
        private const string SideElementName = "inventory-side";
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
        private InventoryArtifactHolder artifactHolder;
        private InventoryUser inventoryUser;
        private VisualElement windowElement;
        private VisualElement bagScrollElement;
        private VisualElement bagGridElement;
        private VisualElement handsGridElement;
        private VisualElement artifactsGridElement;
        private VisualElement artifactsSectionElement;
        private VisualElement sideElement;
        private ItemTooltip itemTooltip;
        private ItemDragging dragging;
        private ItemContextMenu contextMenu;
        private StackSplitPrompt splitPrompt;
        private CraftPanel craftPanel;
        private GivePanel givePanel;
        private LootPanel lootPanel;
        private bool open;
        private bool stale;

        public void ShooterUpdate()
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
            artifactsGridElement = root.Q<VisualElement>(ArtifactsGridElementName);
            artifactsSectionElement = root.Q<VisualElement>(ArtifactsSectionElementName);
            sideElement = root.Q<VisualElement>(SideElementName);
            VisualElement tooltipElement = root.Q<VisualElement>(TooltipElementName);
            Label tooltipTitleLabel = root.Q<Label>(TooltipTitleLabelName);
            Label tooltipDescriptionLabel = root.Q<Label>(TooltipDescriptionLabelName);
            VisualElement tooltipStatsElement = root.Q<VisualElement>(TooltipStatsElementName);
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

            if (windowElement == null || bagScrollElement == null || bagGridElement == null || handsGridElement == null || artifactsGridElement == null || artifactsSectionElement == null || sideElement == null || craftGridElement == null || craftOutputElement == null || givePanelElement == null || giveTitleLabel == null || giveGridElement == null || giveButton == null ||
                lootPanelElement == null || lootTitleLabel == null || lootGridElement == null || lootTakeAllButton == null ||
                tooltipElement == null || tooltipTitleLabel == null || tooltipDescriptionLabel == null || tooltipStatsElement == null)
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
            itemTooltip = new ItemTooltip(tooltipElement, tooltipTitleLabel, tooltipDescriptionLabel, tooltipStatsElement);
            itemTooltip.Hide();
            windowElement.RegisterCallback<PointerMoveEvent>(ShowTooltipAtPointer);
            windowElement.RegisterCallback<PointerLeaveEvent>(_ => itemTooltip.Hide());

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
            artifactHolder = LocalPlayerFinder.FindComponent<InventoryArtifactHolder>();
            inventoryUser = LocalPlayerFinder.FindComponent<InventoryUser>();
            craftPanel.Open(inventory, LocalPlayerFinder.FindComponent<Crafter>());
            OpenOtherSide(loot);

            if (inventory != null) inventory.Changed += MarkStale;
            if (inventoryHolder != null) inventoryHolder.Changed += MarkStale;
            if (artifactHolder != null) artifactHolder.Changed += MarkStale;

            windowElement.style.display = DisplayStyle.Flex;
            stale = true;

            string takerName = givePanel.DescribeTaker();
            Log.Info(loot != null ? $"The bag is open on {loot.name}" : takerName == null ? "The bag is open" : $"The bag is open, {takerName} can take things from it");
        }

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
            if (artifactHolder != null) artifactHolder.Changed -= MarkStale;
            inventory = null;
            inventoryHolder = null;
            artifactHolder = null;
            inventoryUser = null;
            craftPanel.Close();
            givePanel.Close();
            lootPanel.Close();
            splitPrompt.Clear();
            itemTooltip.Hide();

            if (windowElement != null) windowElement.style.display = DisplayStyle.None;
            bagGridElement?.Clear();
            handsGridElement?.Clear();
            artifactsGridElement?.Clear();
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
            artifactsGridElement.Clear();

            ItemGrid.DrawCells(handsGridElement, HandsGridRows, BagGridColumns);
            artifactsSectionElement.style.display = artifactHolder == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (artifactHolder != null) ItemGrid.DrawCells(artifactsGridElement, 1, artifactHolder.MaxSlots);

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
                handsGridElement.Add(CreateUniqueElement(asset, heldItem.AssetId, 0, 0, heldSlot, true, true, false));
            }

            HashSet<int> artifactSlots = DrawArtifacts(itemCatalog);

            GridPlacement placement = new(BagGridRows, BagGridColumns, true);
            List<VisualElement> itemElements = new();

            IReadOnlyList<UniqueItem> uniqueItems = inventory.UniqueItems;
            for (int slot = 0; slot < uniqueItems.Count; slot++)
            {
                UniqueItem uniqueItem = uniqueItems[slot];
                if (uniqueItem == null || slot == heldSlot || artifactSlots.Contains(slot) || givePanel.IsSlotOffered(slot)) continue;

                ItemAsset asset = itemCatalog == null ? null : itemCatalog.GetAssetById(uniqueItem.AssetId);
                placement.TryPlace(ItemGrid.GetSizeInCells(asset), out int row, out int column);
                bool equipable = asset is UniqueItemAsset uniqueItemAsset && uniqueItemAsset.IsEquipable;
                itemElements.Add(CreateUniqueElement(asset, uniqueItem.AssetId, row, column, slot, equipable, false, false));
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

            sideElement.style.display = lootPanel.IsOpen ? DisplayStyle.None : DisplayStyle.Flex;
            craftPanel.Draw();
            givePanel.Draw();
            lootPanel.Draw();
        }

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

        private HashSet<int> DrawArtifacts(ItemCatalog itemCatalog)
        {
            HashSet<int> artifactSlots = new();
            if (artifactHolder == null) return artifactSlots;

            int column = 0;
            foreach (int slot in artifactHolder.HeldSlots)
            {
                artifactSlots.Add(slot);
                UniqueItem artifact = inventory.GetUniqueAt(slot);
                if (artifact == null || givePanel.IsSlotOffered(slot)) continue;

                ItemAsset asset = itemCatalog == null ? null : itemCatalog.GetAssetById(artifact.AssetId);
                artifactsGridElement.Add(CreateUniqueElement(asset, artifact.AssetId, 0, column, slot, false, false, true));
                column += ItemGrid.GetSizeInCells(asset).x;
            }

            return artifactSlots;
        }

        private void ShowTooltipAtPointer(PointerMoveEvent pointerMove)
        {
            ItemAsset asset = dragging.IsDragging ? null : FindPointedAsset(pointerMove.target as VisualElement);
            if (asset == null)
            {
                itemTooltip.Hide();
                return;
            }

            itemTooltip.Show(asset);
            itemTooltip.MoveTo(windowElement.WorldToLocal(pointerMove.position), windowElement.contentRect);
        }

        private ItemAsset FindPointedAsset(VisualElement target)
        {
            for (VisualElement element = target; element != null && element != windowElement; element = element.parent)
            {
                if (element.ClassListContains("slot")) return element.userData as ItemAsset;
            }

            return null;
        }

        private VisualElement CreateUniqueElement(ItemAsset asset, string fallbackTitle, int row, int column, int slot, bool equipable, bool inHands, bool inArtifacts)
        {
            Vector2 size = ItemGrid.GetSize(asset);
            VisualElement itemElement = ItemGrid.CreateSlotElement(asset, fallbackTitle, inHands || inArtifacts, equipable || asset is ArtifactAsset);
            ItemGrid.PlaceAt(itemElement, row, column, size);
            dragging.MakeDraggable(itemElement, asset, size, DraggedItem.Unique(slot, equipable, inHands, inArtifacts));

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

            bool isOverArtifacts = artifactHolder != null && artifactsGridElement.worldBound.Contains(position);
            bool isOverBag = bagScrollElement.worldBound.Contains(position) || handsGridElement.worldBound.Contains(position) || isOverArtifacts;
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

            if (isOverArtifacts && !item.IsFromArtifacts)
            {
                artifactHolder.HoldSlotRpc(item.UniqueSlot);
                return;
            }

            if (item.IsFromArtifacts)
            {
                if (bagScrollElement.worldBound.Contains(position)) artifactHolder.ReleaseSlotRpc(item.UniqueSlot);
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
