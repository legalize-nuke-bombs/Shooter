using System.Collections.Generic;
using System.Globalization;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    // The card at the foot of the bag: the picture, kind, name, description and numbers of the thing last pointed at;
    // it keeps its size empty or full, so the bag does not jump under the pointer
    public sealed class ItemCard
    {
        private const string EmptyClass = "card--empty";

        private readonly VisualElement cardElement;
        private readonly VisualElement iconElement;
        private readonly Label kindLabel;
        private readonly Label titleLabel;
        private readonly Label descriptionLabel;
        private readonly VisualElement statsElement;

        public ItemCard(VisualElement cardElement, VisualElement iconElement, Label kindLabel, Label titleLabel, Label descriptionLabel, VisualElement statsElement)
        {
            this.cardElement = cardElement;
            this.iconElement = iconElement;
            this.kindLabel = kindLabel;
            this.titleLabel = titleLabel;
            this.descriptionLabel = descriptionLabel;
            this.statsElement = statsElement;
        }

        public ItemAsset Shown { get; private set; }

        public void Show(ItemAsset asset)
        {
            if (asset == null)
            {
                Clear();
                return;
            }

            if (asset == Shown) return;

            Shown = asset;
            Sprite iconSprite = ItemGrid.GetIconSprite(asset);
            iconElement.style.backgroundImage = iconSprite == null ? new StyleBackground(StyleKeyword.None) : new StyleBackground(iconSprite);
            kindLabel.text = DescribeKind(asset);
            titleLabel.text = asset.Title;
            descriptionLabel.text = asset.Description;
            descriptionLabel.style.display = string.IsNullOrEmpty(asset.Description) ? DisplayStyle.None : DisplayStyle.Flex;

            statsElement.Clear();
            foreach ((string name, string value) in DescribeNumbers(asset))
            {
                statsElement.Add(CreateStat(name, value));
            }

            cardElement.RemoveFromClassList(EmptyClass);
        }

        public void Clear()
        {
            Shown = null;
            iconElement.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            kindLabel.text = string.Empty;
            titleLabel.text = string.Empty;
            descriptionLabel.text = string.Empty;
            statsElement.Clear();
            cardElement.AddToClassList(EmptyClass);
        }

        private static string DescribeKind(ItemAsset asset)
        {
            switch (asset)
            {
                case FirearmAsset:
                    return "ОРУЖИЕ · В РУКИ";
                case ArtifactAsset:
                    return "АРТЕФАКТ · НА ПОЯС";
                case StackableItemAsset stackable when FindFirearmFor(stackable) != null:
                    return "ПАТРОНЫ";
                case StackableItemAsset { IsUsable: true }:
                    return "РАСХОДНИК";
                default:
                    return "ВЕЩЬ";
            }
        }

        private static IEnumerable<(string Name, string Value)> DescribeNumbers(ItemAsset asset)
        {
            switch (asset)
            {
                case FirearmAsset firearm:
                    yield return ("Урон", firearm.Damage.ToString(CultureInfo.InvariantCulture));
                    yield return ("Магазин", firearm.MagazineSize.ToString(CultureInfo.InvariantCulture));
                    if (firearm.Ammo != null) yield return (string.Empty, firearm.Ammo.Title);
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
            statElement.AddToClassList("card__stat");

            if (!string.IsNullOrEmpty(name))
            {
                Label nameLabel = new(name);
                nameLabel.AddToClassList("card__stat-name");
                statElement.Add(nameLabel);
            }

            Label valueLabel = new(value);
            valueLabel.AddToClassList("card__stat-value");
            statElement.Add(valueLabel);

            return statElement;
        }
    }
}
