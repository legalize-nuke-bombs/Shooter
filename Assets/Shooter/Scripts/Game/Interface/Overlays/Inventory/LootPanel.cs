using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    // The other side of the bag window while a chest or a corpse is searched: what lies in it, which a drag into the bag
    // or Take all takes, and what a drag from the bag onto it puts there; nothing waits for consent, the server does it
    public sealed class LootPanel
    {
        private const int GridColumns = 10;
        private const int GridRows = 7;
        private static readonly Journal Log = Logs.Here();

        private readonly VisualElement panelElement;
        private readonly Label titleLabel;
        private readonly VisualElement gridElement;
        private readonly Button takeAllButton;
        private readonly ItemDragging dragging;
        private readonly Action markStale;
        private Lootable loot;
        private Inventory lootInventory;
        private InventoryExchanger inventoryExchanger;
        private int shownRows = GridRows;

        public LootPanel(VisualElement panelElement, Label titleLabel, VisualElement gridElement, Button takeAllButton, ItemDragging dragging, Action markStale)
        {
            this.panelElement = panelElement;
            this.titleLabel = titleLabel;
            this.gridElement = gridElement;
            this.takeAllButton = takeAllButton;
            this.dragging = dragging;
            this.markStale = markStale;

            panelElement.style.display = DisplayStyle.None;
            takeAllButton.clicked += TakeAll;
            gridElement.GetFirstAncestorOfType<ScrollView>().RegisterCallback<GeometryChangedEvent>(FitRowsToHeight);
        }

        public Lootable Loot => loot;

        public bool IsOpen => loot != null;

        // Out of reach the search is over, as it is when the loot is gone
        public bool IsInReach => loot != null && loot.gameObject.activeInHierarchy &&
                                 Vector3.Distance(inventoryExchanger.transform.position, loot.transform.position) <= inventoryExchanger.Radius;

        private long LootId => loot.GetComponent<IncrementalId>().Value;

        public void Unbind()
        {
            takeAllButton.clicked -= TakeAll;
        }

        public void Open(Lootable openedLoot, InventoryExchanger openedExchanger)
        {
            loot = openedLoot;
            lootInventory = openedLoot.GetComponent<Inventory>();
            inventoryExchanger = openedExchanger;
            lootInventory.Changed += markStale;
            Log.Info($"The bag searches {loot.name}");
        }

        public void Close()
        {
            if (lootInventory != null) lootInventory.Changed -= markStale;

            loot = null;
            lootInventory = null;
            inventoryExchanger = null;
            panelElement.style.display = DisplayStyle.None;
            gridElement.Clear();
        }

        public bool IsOver(Vector2 position)
        {
            return loot != null && panelElement.worldBound.Contains(position);
        }

        public void Draw()
        {
            gridElement.Clear();
            panelElement.style.display = loot == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (loot == null) return;

            titleLabel.text = loot.ShownName;

            // The grid grows by rows when the things do not fit, and its panel scrolls
            ItemCatalog itemCatalog = Catalogs.GetCatalog<ItemCatalog>();
            GridPlacement placement = new(shownRows, GridColumns, true);
            List<VisualElement> itemElements = new();

            IReadOnlyList<UniqueItem> uniqueItems = lootInventory.UniqueItems;
            for (int slot = 0; slot < uniqueItems.Count; slot++)
            {
                UniqueItem uniqueItem = uniqueItems[slot];
                if (uniqueItem == null) continue;

                itemElements.Add(CreateItemElement(itemCatalog.GetAssetById(uniqueItem.AssetId), uniqueItem.AssetId, placement, DraggedItem.LootUnique(slot), 0));
            }

            for (int index = 0; index < itemCatalog.Count; index++)
            {
                if (itemCatalog.GetAssetAt(index) is not StackableItemAsset asset) continue;

                int amount = lootInventory.CountStackable(asset);
                if (amount > 0) itemElements.Add(CreateItemElement(asset, asset.Id, placement, DraggedItem.LootStack(asset), amount));
            }

            ItemGrid.DrawCells(gridElement, placement.Rows, GridColumns);
            foreach (VisualElement itemElement in itemElements)
            {
                gridElement.Add(itemElement);
            }

            takeAllButton.SetEnabled(itemElements.Count > 0);
        }

        public void Take(DraggedItem item)
        {
            if (item.LootStackableAsset != null)
            {
                inventoryExchanger.TakeStackableRpc(LootId, item.LootStackableAsset.NetworkId, lootInventory.CountStackable(item.LootStackableAsset));
            }
            else
            {
                inventoryExchanger.TakeUniqueRpc(LootId, item.LootSlot);
            }
        }

        public void PutStackable(StackableItemAsset asset, int amount)
        {
            inventoryExchanger.GiveStackableRpc(LootId, asset.NetworkId, amount);
        }

        public void PutUnique(int slot)
        {
            inventoryExchanger.GiveUniqueRpc(LootId, slot);
        }

        // The panel stands as tall as the bag beside it, and its grid fills it with as many rows as fit
        private void FitRowsToHeight(GeometryChangedEvent change)
        {
            int rows = Mathf.Max(GridRows, Mathf.FloorToInt(change.newRect.height / ItemGrid.CellSize));
            if (rows == shownRows) return;

            shownRows = rows;
            markStale();
        }

        private VisualElement CreateItemElement(ItemAsset asset, string fallbackTitle, GridPlacement placement, DraggedItem item, int amount)
        {
            placement.TryPlace(ItemGrid.GetSizeInCells(asset), out int row, out int column);

            Vector2 size = ItemGrid.GetSize(asset);
            VisualElement itemElement = ItemGrid.CreateSlotElement(asset, fallbackTitle, false, false);
            ItemGrid.PlaceAt(itemElement, row, column, size);
            if (amount > 0) ItemGrid.AddAmountLabel(itemElement, amount);
            dragging.MakeDraggable(itemElement, asset, size, item);

            return itemElement;
        }

        private void TakeAll()
        {
            if (loot == null) return;

            int takenCount = 0;
            IReadOnlyList<UniqueItem> uniqueItems = lootInventory.UniqueItems;
            for (int slot = 0; slot < uniqueItems.Count; slot++)
            {
                if (uniqueItems[slot] == null) continue;

                inventoryExchanger.TakeUniqueRpc(LootId, slot);
                takenCount++;
            }

            ItemCatalog itemCatalog = Catalogs.GetCatalog<ItemCatalog>();
            for (int index = 0; index < itemCatalog.Count; index++)
            {
                if (itemCatalog.GetAssetAt(index) is not StackableItemAsset asset) continue;

                int amount = lootInventory.CountStackable(asset);
                if (amount <= 0) continue;

                inventoryExchanger.TakeStackableRpc(LootId, asset.NetworkId, amount);
                takenCount++;
            }

            Log.Info($"The bag takes all {takenCount} things from {loot.name}");
        }
    }
}
