using Unity.Collections;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : Catalog<ItemSpec>
    {
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
    }
}
