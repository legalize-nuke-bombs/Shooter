using System.Collections.Generic;
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

        public bool Usable => effects != null && effects.Length > 0;
    }
}
