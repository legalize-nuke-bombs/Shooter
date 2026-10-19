using Unity.Collections;
using UnityEngine;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Icon Catalog", fileName = "IconCatalog")]
    public class IconCatalog : Catalog<IconAsset>
    {
        public Sprite Sprite(FixedString32Bytes id)
        {
            IconAsset asset = GetAssetById(id);

            return asset == null ? null : asset.Sprite;
        }
    }
}
