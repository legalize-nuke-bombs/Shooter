using Unity.Collections;
using UnityEngine;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Icon Catalog", fileName = "IconCatalog")]
    public class IconCatalog : Catalog<IconAsset>
    {
        public Sprite GetSpriteById(FixedString32Bytes id)
        {
            IconAsset iconAsset = GetAssetById(id);

            return iconAsset == null ? null : iconAsset.Sprite;
        }
    }
}
