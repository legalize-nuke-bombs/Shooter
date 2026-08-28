using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : Catalog<ItemSpec>
    {
        private static readonly Journal Log = Logs.Here();

        public int Kind(UniqueItem item)
        {
            if (item == null) return -1;

            int kind = Index(Spec(item.SpecId));
            if (kind < 0) Log.Error($"Catalog {name} has no place for a thing of kind {item.SpecId}");

            return kind;
        }

        public UniqueItem Create(int kind)
        {
            if (At(kind) is UniqueItemSpec spec) return spec.Create();

            Log.Error($"Catalog {name} has no unique item under index {kind}");

            return null;
        }

        public ItemSpec Spec(string id)
        {
            return Of(new FixedString32Bytes(id));
        }

        public FirearmSpec Firearm(string id)
        {
            return Of(new FixedString32Bytes(id)) as FirearmSpec;
        }
    }
}
