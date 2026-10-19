using Shooter.Infrastructure;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : Catalog<ItemAsset>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        public int GetAssetIndexOf(UniqueItem item)
        {
            if (item == null) return -1;

            int assetIndex = IndexOf(GetAssetById(item.AssetId));
            if (assetIndex < 0) Log.Error($"Catalog {name} has no place for a thing of kind {item.AssetId}");

            return assetIndex;
        }

        public UniqueItem CreateUniqueItemAt(int assetIndex)
        {
            if (GetAssetAt(assetIndex) is UniqueItemAsset asset) return asset.CreateItem();

            Log.Error($"Catalog {name} has no unique item under index {assetIndex}");

            return null;
        }

        public ItemAsset GetAssetById(string id)
        {
            return base.GetAssetById(new FixedString32Bytes(id));
        }

        public FirearmAsset GetFirearmAssetById(string id)
        {
            return base.GetAssetById(new FixedString32Bytes(id)) as FirearmAsset;
        }
    }
}
