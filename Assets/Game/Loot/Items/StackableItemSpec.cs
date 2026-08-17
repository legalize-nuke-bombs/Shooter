using System.Collections.Generic;
using System.Linq;
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
        public int FoodMarker => effects.Length == 0 ? 0 : effects.Max(effect => effect.FoodMarker);
        public int HealMarker => effects.Length == 0 ? 0 : effects.Max(effect => effect.HealMarker);
    }
}
