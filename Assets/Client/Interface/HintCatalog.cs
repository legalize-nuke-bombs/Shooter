using System;
using System.Collections.Generic;
using Shooter.Game.Body;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Client.Interface
{
    [CreateAssetMenu(menuName = "Shooter/Hint Catalog", fileName = "HintCatalog")]
    public sealed class HintCatalog : ScriptableObject
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Hint[] hints;

        private readonly HashSet<UsageType> unhinted = new HashSet<UsageType>();

        public string Text(UsageType usage)
        {
            foreach (Hint hint in hints)
            {
                if (hint.Usage == usage) return hint.Text;
            }

            if (unhinted.Add(usage)) Log.Warn($"Hint catalog {name} has no hint for {usage}");

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
