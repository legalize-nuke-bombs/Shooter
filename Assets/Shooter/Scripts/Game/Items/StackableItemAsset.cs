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
        public override ItemStats Stats => effects.Aggregate(new ItemStats(), (sum, effect) => sum + effect.Stats);
        public override string PromptDescription => IsUsable ? base.PromptDescription + " Usable." : base.PromptDescription;
    }
}
