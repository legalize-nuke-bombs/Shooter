using Shooter.Logging;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : Catalog<ItemSpec>
    {
        private static readonly Journal Log = Logs.Here();

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
