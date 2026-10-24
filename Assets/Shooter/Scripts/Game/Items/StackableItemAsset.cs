using System.Collections.Generic;
using System.Linq;
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
        public override IEnumerable<ItemStat> Stats => effects.Select(effect => effect.Stat);
        public override string PromptDescription => IsUsable ? base.PromptDescription + " Usable." : base.PromptDescription;
    }
}
