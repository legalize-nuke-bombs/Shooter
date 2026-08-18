using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Body;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Stackable Item", fileName = "StackableItem")]
    public class StackableItemSpec : ItemSpec
    {
        [SerializeField] private ItemEffect[] effects = { };
        [SerializeField] private SoundSpec useSound;

        public IReadOnlyList<ItemEffect> Effects => effects;

        public SoundSpec UseSound => useSound;

        public bool Usable => effects.Length > 0;
        public int HealMarker => effects.Length == 0 ? 0 : effects.Sum(effect => effect.HealMarker);
        public int FoodMarker => effects.Length == 0 ? 0 : effects.Sum(effect => effect.FoodMarker);
        public override string PromptDescription
        {
            get
            {
                var sb = new StringBuilder();

                sb.Append(base.PromptDescription);

                if (Usable) sb.Append(" Usable.");
                if (HealMarker > 0) sb.Append($" Heal {HealMarker}.");
                if (FoodMarker > 0) sb.Append($" Food {FoodMarker}.");

                return sb.ToString();
            }
        }
    }
}
