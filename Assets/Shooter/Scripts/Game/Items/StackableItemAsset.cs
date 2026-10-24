using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Stackable Item", fileName = "StackableItem")]
    public class StackableItemAsset : ItemAsset
    {
        [SerializeField] private ItemEffect[] effects = { };
        [SerializeField] private SoundAsset useSound;

        public IReadOnlyList<ItemEffect> Effects => effects;

        public SoundAsset UseSound => useSound;

        public bool IsUsable => effects.Length > 0;
        public int HealAmount => effects.Length == 0 ? 0 : effects.Sum(effect => effect.HealAmount);
        public int FoodAmount => effects.Length == 0 ? 0 : effects.Sum(effect => effect.FoodAmount);
        public int IntoxicationAmount => effects.Length == 0 ? 0 : effects.Sum(effect => effect.IntoxicationAmount);
        public override string PromptDescription
        {
            get
            {
                StringBuilder description = new();

                description.Append(base.PromptDescription);

                if (IsUsable) description.Append(" Usable.");
                if (HealAmount > 0) description.Append($" Heal {HealAmount}.");
                if (FoodAmount > 0) description.Append($" Food {FoodAmount}.");

                return description.ToString();
            }
        }
    }
}
