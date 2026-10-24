using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    // The tooltip at the pointer while it rests on a thing in the bag window, as in most games: the name, the
    // description and the numbers; it floats over the window, so the window keeps its size whatever it says. Every
    // number has its row in the layout, which names it; a number the thing does not have hides its row
    public sealed class ItemTooltip
    {
        private const float PointerGap = 16f;
        private const string HealRowName = "item-tooltip-heal";
        private const string HealPerHourRowName = "item-tooltip-heal-per-hour";
        private const string FoodRowName = "item-tooltip-food";
        private const string IntoxicationRowName = "item-tooltip-intoxication";
        private const string DamageRowName = "item-tooltip-damage";
        private const string MagazineRowName = "item-tooltip-magazine";
        private const string StatValueClassName = "tooltip__stat-value";

        private readonly VisualElement tooltipElement;
        private readonly Label titleLabel;
        private readonly Label descriptionLabel;
        private readonly VisualElement statsElement;

        public ItemTooltip(VisualElement tooltipElement, Label titleLabel, Label descriptionLabel, VisualElement statsElement)
        {
            this.tooltipElement = tooltipElement;
            this.titleLabel = titleLabel;
            this.descriptionLabel = descriptionLabel;
            this.statsElement = statsElement;
        }

        public ItemAsset Shown { get; private set; }

        public void Show(ItemAsset asset)
        {
            if (asset == Shown) return;

            Shown = asset;
            titleLabel.text = asset.Title;
            descriptionLabel.text = asset.Description;
            descriptionLabel.style.display = string.IsNullOrEmpty(asset.Description) ? DisplayStyle.None : DisplayStyle.Flex;

            ItemStats stats = asset.Stats;
            ShowGain(HealRowName, stats.Heal);
            ShowGain(HealPerHourRowName, stats.HealPerHour);
            ShowGain(FoodRowName, stats.Food);
            ShowGain(IntoxicationRowName, stats.Intoxication);
            ShowNumber(DamageRowName, stats.Damage);
            ShowNumber(MagazineRowName, stats.Magazine);
            statsElement.style.display = stats.Equals(new ItemStats()) ? DisplayStyle.None : DisplayStyle.Flex;

            tooltipElement.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            Shown = null;
            tooltipElement.style.display = DisplayStyle.None;
        }

        // Below and to the right of the pointer, turned to its other side where the window would cut it off
        public void MoveTo(Vector2 pointer, Rect window)
        {
            float width = float.IsNaN(tooltipElement.resolvedStyle.width) ? 0f : tooltipElement.resolvedStyle.width;
            float height = float.IsNaN(tooltipElement.resolvedStyle.height) ? 0f : tooltipElement.resolvedStyle.height;

            float left = pointer.x + PointerGap + width > window.width ? pointer.x - PointerGap - width : pointer.x + PointerGap;
            float top = pointer.y + PointerGap + height > window.height ? pointer.y - PointerGap - height : pointer.y + PointerGap;

            tooltipElement.style.left = Mathf.Max(0f, left);
            tooltipElement.style.top = Mathf.Max(0f, top);
        }

        private void ShowGain(string rowName, int value)
        {
            ShowRow(rowName, value, $"+{value}");
        }

        private void ShowNumber(string rowName, int value)
        {
            ShowRow(rowName, value, $"{value}");
        }

        private void ShowRow(string rowName, int value, string text)
        {
            VisualElement rowElement = statsElement.Q<VisualElement>(rowName);
            rowElement.style.display = value == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            rowElement.Q<Label>(className: StatValueClassName).text = text;
        }
    }
}
