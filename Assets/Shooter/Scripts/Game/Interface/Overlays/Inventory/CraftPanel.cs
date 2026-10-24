using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public sealed class CraftPanel
    {
        private const int Side = 3;

        private readonly ItemStack[] stacks = new ItemStack[Side * Side];
        private readonly VisualElement gridElement;
        private readonly VisualElement outputElement;
        private readonly ItemDragging dragging;
        private readonly Action markStale;
        private Inventory inventory;
        private Crafter crafter;
        private CraftAsset pendingCraft;
        private int outputCountBeforeCraft;

        public CraftPanel(VisualElement gridElement, VisualElement outputElement, ItemDragging dragging, Action markStale)
        {
            this.gridElement = gridElement;
            this.outputElement = outputElement;
            this.dragging = dragging;
            this.markStale = markStale;
        }

        public void Open(Inventory openedInventory, Crafter openedCrafter)
        {
            inventory = openedInventory;
            crafter = openedCrafter;
        }

        public void Close()
        {
            inventory = null;
            crafter = null;
            pendingCraft = null;
            Array.Clear(stacks, 0, stacks.Length);
            gridElement.Clear();
            outputElement.Clear();
        }

        public ItemStack GetStackAt(int cell)
        {
            return stacks[cell];
        }

        public int CountOf(StackableItemAsset asset)
        {
            int count = 0;
            foreach (ItemStack stack in stacks)
            {
                if (stack.Asset == asset) count += stack.Count;
            }

            return count;
        }

        public void PayForArrivedOutput()
        {
            if (pendingCraft == null || CountInInventory(pendingCraft.Output) <= outputCountBeforeCraft) return;

            for (int cell = 0; cell < stacks.Length; cell++)
            {
                if (stacks[cell].Asset != null) TakeFromCell(cell, 1);
            }

            pendingCraft = null;
        }

        public void ReleaseUnbacked(Func<StackableItemAsset, int> countReserved)
        {
            for (int cell = stacks.Length - 1; cell >= 0; cell--)
            {
                StackableItemAsset asset = stacks[cell].Asset;
                if (asset == null) continue;

                int excess = countReserved(asset) - inventory.CountStackable(asset);
                if (excess > 0) TakeFromCell(cell, Math.Min(excess, stacks[cell].Count));
            }
        }

        public void Draw()
        {
            gridElement.Clear();
            outputElement.Clear();

            ItemGrid.DrawCells(gridElement, Side, Side);
            ItemGrid.DrawCells(outputElement, 1, 1);

            for (int cell = 0; cell < stacks.Length; cell++)
            {
                ItemStack stack = stacks[cell];
                if (stack.Asset == null) continue;

                VisualElement itemElement = CreateOneCellElement(stack.Asset, cell / Side, cell % Side);
                if (stack.Count > 1) ItemGrid.AddAmountLabel(itemElement, stack.Count);

                dragging.MakeDraggable(itemElement, stack.Asset, ItemGrid.OneCellSize, DraggedItem.FromCraftCell(cell));
                int clickedCell = cell;
                itemElement.RegisterCallback<PointerDownEvent>(pointerDown =>
                {
                    if (pointerDown.button != 1 || dragging.IsDragging) return;

                    stacks[clickedCell] = default;
                    markStale();
                    pointerDown.StopPropagation();
                });
                gridElement.Add(itemElement);
            }

            CraftAsset matchingCraft = FindMatchingCraft();
            outputElement.EnableInClassList("craft__output--ready", matchingCraft != null);
            if (matchingCraft == null) return;

            VisualElement outputItemElement = CreateOneCellElement(matchingCraft.Output, 0, 0);
            dragging.MakeDraggable(outputItemElement, matchingCraft.Output, ItemGrid.OneCellSize, DraggedItem.CraftOutput());
            outputElement.Add(outputItemElement);
        }

        public int FindCellAt(Vector2 position)
        {
            Rect bounds = gridElement.worldBound;
            if (!bounds.Contains(position)) return -1;

            int column = Mathf.Clamp((int)((position.x - bounds.x) / ItemGrid.CellSize), 0, Side - 1);
            int row = Mathf.Clamp((int)((position.y - bounds.y) / ItemGrid.CellSize), 0, Side - 1);

            return row * Side + column;
        }

        public bool TryPut(int cell, StackableItemAsset asset, int amount)
        {
            if (stacks[cell].Asset == null)
            {
                stacks[cell] = new ItemStack { Asset = asset, Count = amount };
            }
            else if (stacks[cell].Asset == asset)
            {
                stacks[cell].Count += amount;
            }
            else
            {
                return false;
            }

            return true;
        }

        public void Empty(int cell)
        {
            stacks[cell] = default;
        }

        public void Move(int fromCell, int toCell)
        {
            if (toCell == fromCell) return;

            if (stacks[toCell].Asset == null)
            {
                stacks[toCell] = stacks[fromCell];
                stacks[fromCell] = default;
            }
            else if (stacks[toCell].Asset == stacks[fromCell].Asset)
            {
                stacks[toCell].Count += stacks[fromCell].Count;
                stacks[fromCell] = default;
            }
        }

        public void Craft()
        {
            CraftAsset matchingCraft = FindMatchingCraft();
            if (matchingCraft == null) return;

            pendingCraft = matchingCraft;
            outputCountBeforeCraft = CountInInventory(matchingCraft.Output);
            crafter.CraftRpc(matchingCraft.NetworkId);
        }

        private void TakeFromCell(int cell, int count)
        {
            stacks[cell].Count -= count;
            if (stacks[cell].Count <= 0) stacks[cell] = default;
        }

        private int CountInInventory(ItemAsset asset)
        {
            if (inventory == null) return 0;
            if (asset is StackableItemAsset stackableAsset) return inventory.CountStackable(stackableAsset);

            int uniqueItemCount = 0;
            foreach (UniqueItem uniqueItem in inventory.UniqueItems)
            {
                if (uniqueItem != null) uniqueItemCount++;
            }

            return uniqueItemCount;
        }

        // The bounding box of the filled cells against the recipe's, so a one-cell recipe matches in any cell
        private CraftAsset FindMatchingCraft()
        {
            if (crafter == null || inventory == null) return null;

            StackableItemAsset[] placedAssets = new StackableItemAsset[stacks.Length];
            for (int cell = 0; cell < stacks.Length; cell++)
            {
                placedAssets[cell] = stacks[cell].Asset;
            }

            if (!TryCropToFilledCells(placedAssets, out StackableItemAsset[] placedShape, out int width, out int height)) return null;

            foreach (CraftAsset craft in crafter.AvailableCrafts)
            {
                if (craft == null || craft.Output == null || craft.Input == null || craft.Input.Length != stacks.Length) continue;
                if (!TryCropToFilledCells(craft.Input, out StackableItemAsset[] recipeShape, out int recipeWidth, out int recipeHeight)) continue;
                if (recipeWidth != width || recipeHeight != height) continue;

                bool isSameShape = true;
                for (int i = 0; i < placedShape.Length && isSameShape; i++)
                {
                    isSameShape = placedShape[i] == recipeShape[i];
                }

                if (isSameShape) return craft;
            }

            return null;
        }

        private static bool TryCropToFilledCells(StackableItemAsset[] cells, out StackableItemAsset[] shape, out int width, out int height)
        {
            int top = Side, left = Side, bottom = -1, right = -1;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;

                int row = i / Side;
                int column = i % Side;
                top = Math.Min(top, row);
                bottom = Math.Max(bottom, row);
                left = Math.Min(left, column);
                right = Math.Max(right, column);
            }

            shape = null;
            width = 0;
            height = 0;
            if (bottom < 0) return false;

            width = right - left + 1;
            height = bottom - top + 1;
            shape = new StackableItemAsset[width * height];
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    shape[row * width + column] = cells[(top + row) * Side + left + column];
                }
            }

            return true;
        }

        private static VisualElement CreateOneCellElement(ItemAsset asset, int row, int column)
        {
            VisualElement itemElement = ItemGrid.CreateSlotElement(asset, asset.Id, false, true);
            ItemGrid.PlaceAt(itemElement, row, column, ItemGrid.OneCellSize);
            return itemElement;
        }
    }
}
