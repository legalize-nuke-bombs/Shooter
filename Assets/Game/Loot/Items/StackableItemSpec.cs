using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Stackable Item", fileName = "StackableItem")]
    public class StackableItemSpec : ItemSpec
    {
        [SerializeField] private ItemEffect[] effects = { };

        public IReadOnlyList<ItemEffect> Effects => effects;

        public bool Usable => effects != null && effects.Length > 0;
    }
}
