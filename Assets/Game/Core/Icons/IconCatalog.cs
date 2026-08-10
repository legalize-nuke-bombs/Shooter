using Unity.Collections;
using UnityEngine;

namespace Shooter.Game.Core
{
    [CreateAssetMenu(menuName = "Shooter/Icon Catalog", fileName = "IconCatalog")]
    public class IconCatalog : Catalog<IconSpec>
    {
        public Sprite Sprite(FixedString32Bytes id)
        {
            IconSpec spec = Of(id);

            return spec == null ? null : spec.Sprite;
        }
    }
}
