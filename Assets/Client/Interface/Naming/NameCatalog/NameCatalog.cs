using System.Collections.Generic;
using Shooter.Game.Body;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Client.Interface.Naming
{
    [CreateAssetMenu(menuName = "Shooter/Name Catalog", fileName = "NameCatalog")]
    public sealed class NameCatalog : ScriptableObject
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private NameSpec[] specs;

        private readonly HashSet<NameableType> unnamed = new HashSet<NameableType>();

        public string Text(NameableType type)
        {
            foreach (NameSpec spec in specs)
            {
                if (spec != null && spec.Type == type) return spec.Text();
            }

            if (unnamed.Add(type)) Log.Warn("Name catalog {} has no name for {}", name, type);

            return type.ToString();
        }
    }
}
