using System;
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
            foreach (StackableItemSpec item in input)
            {
                sb.Append((item == null ? "null" : item.Id) + " ");
            }
            sb.Append("-> ");
            sb.Append(output.Id);
            return sb.ToString();
        }

        public bool Match(List<string> pattern)
        {
            if (pattern.Count != 9)
            {
                throw new ArgumentException("pattern length must be 9");
            }

            for (int i = 0; i < 9; i++)
            {
                bool patternSet = (!String.IsNullOrEmpty(pattern[i]) && pattern[i] != "null");
                bool valueSet = (input[i] != null);

                if (patternSet != valueSet)
                {
                    return false;
                }

                if (patternSet)
                {
                    if (pattern[i] != input[i].Id)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
