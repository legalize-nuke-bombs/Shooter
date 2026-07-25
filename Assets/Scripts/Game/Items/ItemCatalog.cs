using UnityEngine;
using Shooter.Logging;

namespace Shooter.Game.Items
{
    [CreateAssetMenu(menuName = "Shooter/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : ScriptableObject
    {
        [SerializeField] private ItemSpec[] specs;

        public ItemSpec Spec(ItemType type)
        {
            foreach (ItemSpec spec in specs)
            {
                if (spec != null && spec.Type == type) return spec;
            }

            Log.Warn("Item catalog {} has no spec for {}", name, type);
            return null;
        }

        public FirearmSpec Firearm(ItemType type)
        {
            return Spec(type) as FirearmSpec;
        }
    }
}
