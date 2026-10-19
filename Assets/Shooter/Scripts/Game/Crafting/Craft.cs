using System.Collections.Generic;
using System.Text;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Craft", fileName = "Craft")]
    public class Craft : Asset
    {
        [SerializeField] private StackableItemAsset[] input = new StackableItemAsset[9];
        [SerializeField] private ItemAsset output;
        [SerializeField] private SoundAsset sound;

        public StackableItemAsset[] Input => input;
        public ItemAsset Output => output;
        public SoundAsset Sound => sound;

        public Dictionary<StackableItemAsset, int> AmountMap()
        {
            var result = new Dictionary<StackableItemAsset, int>();
            foreach (StackableItemAsset item in input)
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
            sb.Append(Id).Append(": ");

            bool first = true;
            foreach (KeyValuePair<StackableItemAsset, int> amount in AmountMap())
            {
                if (!first) sb.Append(", ");
                sb.Append(amount.Key.Id).Append(" x ").Append(amount.Value);
                first = false;
            }
            if (first) sb.Append("nothing");

            sb.Append(" -> ").Append(output == null ? "nothing" : output.Id);
            return sb.ToString();
        }
    }
}
