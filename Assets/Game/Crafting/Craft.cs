using System.Collections.Generic;
using System.Text;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using UnityEngine;

namespace Shooter.Game.Crafting
{
    [CreateAssetMenu(menuName = "Shooter/Craft", fileName = "Craft")]
    public class Craft : Spec
    {
        [SerializeField] private StackableItemSpec[] input = new StackableItemSpec[9];
        [SerializeField] private ItemSpec output;

        public StackableItemSpec[] Input => input;
        public ItemSpec Output => output;

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

        public string PromptDescription()
        {
            var sb = new StringBuilder();
            foreach (StackableItemSpec item in input)
            {
                sb.Append((item == null ? "null" : item.Id) + " ");
            }
            sb.Append("-> ");
            sb.Append(output.Id);
            return sb.ToString();
        }
    }
}
