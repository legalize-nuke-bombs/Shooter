using System;
using System.Collections.Generic;
using UnityEngine;
using Shooter.Game.Body;
using Shooter.Logging;

namespace Shooter.Client.Interface
{
    [CreateAssetMenu(menuName = "Shooter/Hint Catalog", fileName = "HintCatalog")]
    public sealed class HintCatalog : ScriptableObject
    {
        [SerializeField] private Hint[] hints;

        private readonly HashSet<UsageType> unhinted = new HashSet<UsageType>();

        public string Text(UsageType usage)
        {
            foreach (Hint hint in hints)
            {
                if (hint.Usage == usage) return hint.Text;
            }

            if (unhinted.Add(usage)) Log.Warn("Hint catalog {} has no hint for {}", name, usage);

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
