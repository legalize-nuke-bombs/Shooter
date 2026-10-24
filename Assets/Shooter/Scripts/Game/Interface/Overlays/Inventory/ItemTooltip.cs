using System.Collections.Generic;
using System.Globalization;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    // The tooltip at the pointer while it rests on a thing in the bag window, as in most games: the name, the kind,
    // the description and the numbers; it floats over the window, so the window keeps its size whatever it says
    public sealed class ItemTooltip
    {
        private const float PointerGap = 16f;

        private readonly VisualElement tooltipElement;
        private readonly Label titleLabel;
        private readonly Label kindLabel;
        private readonly Label descriptionLabel;
        private readonly VisualElement statsElement;

        public ItemTooltip(VisualElement tooltipElement, Label titleLabel, Label kindLabel, Label descriptionLabel, VisualElement statsElement)
        {
            this.tooltipElement = tooltipElement;
            this.titleLabel = titleLabel;
            this.kindLabel = kindLabel;
            this.descriptionLabel = descriptionLabel;
            this.statsElement = statsElement;
        }

        public ItemAsset Shown { get; private set; }

        public void Show(ItemAsset asset)
        {
            if (asset == Shown) return;

            Shown = asset;
            titleLabel.text = asset.Title;
            kindLabel.text = DescribeKind(asset);
            descriptionLabel.text = asset.Description;
            descriptionLabel.style.display = string.IsNullOrEmpty(asset.Description) ? DisplayStyle.None : DisplayStyle.Flex;

            statsElement.Clear();
            foreach ((string name, string value) in DescribeNumbers(asset))
            {
                statsElement.Add(CreateStat(name, value));
            }

            statsElement.style.display = statsElement.childCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
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

        private static string DescribeKind(ItemAsset asset)
        {
            switch (asset)
            {
                case FirearmAsset:
                    return "Оружие, берётся в руки";
                case ArtifactAsset:
                    return "Артефакт, носится на поясе";
                case StackableItemAsset stackable when FindFirearmFor(stackable) != null:
                    return "Патроны";
                case StackableItemAsset { IsUsable: true }:
                    return "Расходник";
                default:
                    return "Вещь";
            }
        }

        private static IEnumerable<(string Name, string Value)> DescribeNumbers(ItemAsset asset)
        {
            switch (asset)
            {
                case FirearmAsset firearm:
                    yield return ("Урон", firearm.Damage.ToString(CultureInfo.InvariantCulture));
                    yield return ("Магазин", firearm.MagazineSize.ToString(CultureInfo.InvariantCulture));
                    if (firearm.Ammo != null) yield return ("Заряжается", firearm.Ammo.Title);
                    break;
                case ArtifactAsset artifact:
                    float healPerSecond = 0f;
                    foreach (ArtifactEffectAsset effect in artifact.Effects)
                    {
                        healPerSecond += effect.HealPerSecond;
                    }

                    if (healPerSecond > 0f) yield return ("Здоровье", "+" + (healPerSecond * 60f).ToString("0.#", CultureInfo.InvariantCulture) + " в минуту");
                    break;
                case StackableItemAsset stackable:
                    FirearmAsset firearmFor = FindFirearmFor(stackable);
                    if (firearmFor != null) yield return ("Для", firearmFor.Title);
                    if (stackable.HealAmount > 0) yield return ("Здоровье", "+" + stackable.HealAmount);
                    if (stackable.FoodAmount > 0) yield return ("Сытость", "+" + stackable.FoodAmount);
                    if (stackable.IntoxicationAmount > 0) yield return ("Опьянение", "+" + stackable.IntoxicationAmount);
                    break;
            }
        }

        private static FirearmAsset FindFirearmFor(StackableItemAsset ammo)
        {
            ItemCatalog itemCatalog = Catalogs.GetCatalog<ItemCatalog>();
            List<ItemAsset> firearms = itemCatalog == null ? new List<ItemAsset>() : itemCatalog.FindAll(asset => asset is FirearmAsset firearm && firearm.Ammo == ammo);

            return firearms.Count == 0 ? null : (FirearmAsset)firearms[0];
        }

        private static VisualElement CreateStat(string name, string value)
        {
            VisualElement statElement = new();
            statElement.AddToClassList("tooltip__stat");

            Label nameLabel = new(name);
            nameLabel.AddToClassList("tooltip__stat-name");
            statElement.Add(nameLabel);

            Label valueLabel = new(value);
            valueLabel.AddToClassList("tooltip__stat-value");
            statElement.Add(valueLabel);

            return statElement;
        }
    }
}
