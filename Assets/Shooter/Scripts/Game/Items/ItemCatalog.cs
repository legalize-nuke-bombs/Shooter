using Shooter.Infrastructure;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : Catalog<ItemAsset>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        public int Kind(UniqueItem item)
        {
            if (item == null) return -1;

            int kind = IndexOf(Asset(item.AssetId));
            if (kind < 0) Log.Error($"Catalog {name} has no place for a thing of kind {item.AssetId}");

            return kind;
        }

        public UniqueItem Create(int kind)
        {
            if (GetAssetAt(kind) is UniqueItemAsset asset) return asset.Create();

            Log.Error($"Catalog {name} has no unique item under index {kind}");

            return null;
        }

        public ItemAsset Asset(string id)
        {
            return GetAssetById(new FixedString32Bytes(id));
        }

        public FirearmAsset Firearm(string id)
        {
            return GetAssetById(new FixedString32Bytes(id)) as FirearmAsset;
        }
    }
}
