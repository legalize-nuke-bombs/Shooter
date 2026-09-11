using System.Collections.Generic;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using UnityEngine;

namespace Shooter.Game.Crafting
{
    [CreateAssetMenu(menuName = "Shooter/Craft", fileName = "Craft")]
    public class Craft : Spec
    {
        [SerializeField] private StackableItemSpec[] input = new StackableItemSpec[9];
        [SerializeField] private StackableItemSpec output;

        public StackableItemSpec[] Input => input;
        public StackableItemSpec Output => output;

        public Dictionary<StackableItemSpec, int> AmountMap()
        {
            var result = new Dictionary<StackableItemSpec, int>();
            foreach (StackableItemSpec item in input)
            {
                result.TryAdd(item, 0);
                result[item]++;
            }
            return result;
        }
    }
}
