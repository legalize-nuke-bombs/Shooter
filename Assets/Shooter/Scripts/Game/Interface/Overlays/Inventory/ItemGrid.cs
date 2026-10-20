using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public static class ItemGrid
    {
        public const float CellSize = 48f;
        private const float IconBezel = 8f;

        public static readonly Vector2 OneCellSize = new(CellSize, CellSize);

        public static void DrawCells(VisualElement gridElement, int rows, int columns)
        {
            gridElement.style.width = columns * CellSize;
            gridElement.style.height = rows * CellSize;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    VisualElement cellElement = new();
                    cellElement.AddToClassList("grid__cell");
                    cellElement.style.left = column * CellSize;
                    cellElement.style.top = row * CellSize;
                    cellElement.style.width = CellSize;
                    cellElement.style.height = CellSize;
                    gridElement.Add(cellElement);
                }
            }
        }

        public static Vector2Int GetSizeInCells(ItemAsset asset)
        {
            return asset == null ? Vector2Int.one : asset.SizeInCells;
        }

        public static Vector2 GetSize(ItemAsset asset)
        {
            Vector2Int sizeInCells = GetSizeInCells(asset);
            return new Vector2(sizeInCells.x * CellSize, sizeInCells.y * CellSize);
        }

        public static void PlaceAt(VisualElement itemElement, int row, int column, Vector2 size)
        {
            itemElement.style.position = Position.Absolute;
            itemElement.style.left = column * CellSize;
            itemElement.style.top = row * CellSize;
            itemElement.style.width = size.x;
            itemElement.style.height = size.y;
        }

        public static VisualElement CreateSlotElement(ItemAsset asset, string fallbackTitle, bool inHands, bool equipable)
        {
            Vector2Int sizeInCells = GetSizeInCells(asset);

            VisualElement slotElement = new() { tooltip = asset == null ? fallbackTitle : asset.Title };
            slotElement.AddToClassList("slot");
            if (inHands) slotElement.AddToClassList("slot--held");
            if (!equipable) slotElement.AddToClassList("slot--fixed");

            Sprite iconSprite = GetIconSprite(asset);
            if (iconSprite != null)
            {
                VisualElement iconElement = new();
                iconElement.AddToClassList("slot__icon");
                iconElement.style.backgroundImage = Background.FromSprite(iconSprite);
                iconElement.style.width = sizeInCells.x * CellSize - IconBezel;
                iconElement.style.height = sizeInCells.y * CellSize - IconBezel;
                slotElement.Add(iconElement);
            }
            else
            {
                Label nameLabel = new(asset == null ? fallbackTitle : asset.Title);
                nameLabel.AddToClassList("slot__name");
                slotElement.Add(nameLabel);
            }

            return slotElement;
        }

        public static void AddAmountLabel(VisualElement itemElement, int count)
        {
            Label amountLabel = new(count.ToString());
            amountLabel.AddToClassList("slot__amount");
            itemElement.Add(amountLabel);
        }

        public static Sprite GetIconSprite(ItemAsset asset)
        {
            return asset == null || asset.Icon == null ? null : asset.Icon.Sprite;
        }
    }
}
