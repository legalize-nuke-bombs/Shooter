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

        public FirearmSpec Firearm(FixedString32Bytes id)
        {
            return Of(id) as FirearmSpec;
        }
    }
}
