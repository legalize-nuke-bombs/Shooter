using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Hint Catalog", fileName = "HintCatalog")]
    public sealed class HintCatalog : ScriptableObject
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private Hint[] hints;

        private readonly HashSet<UsageType> usagesWithoutHint = new();

        public string GetHintText(UsageType usage)
        {
            foreach (Hint hint in hints)
            {
                if (hint.Usage == usage) return hint.Text;
            }

            if (usagesWithoutHint.Add(usage)) Log.Error($"Hint catalog {name} has no hint for {usage}");

            return usage.ToString();
        }

        [Serializable]
        private struct Hint
        {
            public UsageType Usage;
            public string Text;
        }
    }
}
