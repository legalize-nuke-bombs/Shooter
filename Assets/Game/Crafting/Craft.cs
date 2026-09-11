using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
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
        [SerializeField] private SoundSpec sound;

        public StackableItemSpec[] Input => input;
        public ItemSpec Output => output;
        public SoundSpec Sound => sound;

        public Dictionary<StackableItemSpec, int> AmountMap()
        {
            var result = new Dictionary<StackableItemSpec, int>();
            foreach (StackableItemSpec item in input)
            {
                if (item == null)
                {
                    continue;
                }
                result.TryAdd(item, 0);
                result[item]++;
            }
            return result;
        }

        public string PromptDescription()
        {
            var sb = new StringBuilder();
            sb.Append(Key).Append(": ");

            bool first = true;
            foreach (KeyValuePair<StackableItemSpec, int> amount in AmountMap())
            {
                if (!first) sb.Append(", ");
                sb.Append(amount.Key.Key).Append(" x ").Append(amount.Value);
                first = false;
            }
            if (first) sb.Append("nothing");

            sb.Append(" -> ").Append(output == null ? "nothing" : output.Key);
            return sb.ToString();
        }
    }
}
