using System.Collections.Generic;
using System.Text;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Craft", fileName = "Craft")]
    public class CraftAsset : Asset
    {
        [SerializeField] private StackableItemAsset[] input = new StackableItemAsset[9];
        [SerializeField] private ItemAsset output;
        [SerializeField] private SoundAsset sound;

        public StackableItemAsset[] Input => input;
        public ItemAsset Output => output;
        public SoundAsset Sound => sound;

        public Dictionary<StackableItemAsset, int> CountInputAmounts()
        {
            var amountsByInput = new Dictionary<StackableItemAsset, int>();
            foreach (StackableItemAsset inputItem in input)
            {
                if (inputItem == null) continue;

                amountsByInput.TryAdd(inputItem, 0);
                amountsByInput[inputItem]++;
            }
            return amountsByInput;
        }

        public string DescribeForPrompt()
        {
            StringBuilder description = new();
            description.Append(Id).Append(": ");

            bool isFirstInput = true;
            foreach (KeyValuePair<StackableItemAsset, int> inputAmount in CountInputAmounts())
            {
                if (!isFirstInput) description.Append(", ");
                description.Append(inputAmount.Key.Id).Append(" x ").Append(inputAmount.Value);
                isFirstInput = false;
            }
            if (isFirstInput) description.Append("nothing");

            description.Append(" -> ").Append(output == null ? "nothing" : output.Id);
            return description.ToString();
        }
    }
}
