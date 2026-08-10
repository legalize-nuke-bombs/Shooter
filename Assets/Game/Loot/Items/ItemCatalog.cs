using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : Catalog<ItemSpec>, IKinds<UniqueItem>
    {
        private static readonly Journal Log = Logs.Here();

        public int Of(UniqueItem item)
        {
            int kind = item == null ? -1 : Index(Spec(item.SpecId));
            if (kind >= 0) return kind;

            Log.Error($"Catalog {name} has no place for a thing of kind {(item == null ? "null" : item.SpecId)}");

            return 0;
        }

        public UniqueItem Create(int kind)
        {
            if (At(kind) is UniqueItemSpec spec) return spec.Create();

            Log.Error($"Catalog {name} has no unique item under index {kind}");

            return null;
        }

        public ItemSpec Spec(FixedString32Bytes id)
        {
            return Of(id);
        }

        public ItemSpec Spec(string id)
        {
            return Of(new FixedString32Bytes(id));
        }

        public FirearmSpec Firearm(string id)
        {
            return Of(new FixedString32Bytes(id)) as FirearmSpec;
        }

        public ItemSpec FindByPromptName(string promptName)
        {
            ItemSpec found = Find(item => item.PromptName == promptName);

            if (found != null && Find(item => item != found && item.PromptName == promptName) != null)
                Log.Warn($"Catalog {name} holds several items under prompt name {promptName}");

            return found;
        }
    }
}
