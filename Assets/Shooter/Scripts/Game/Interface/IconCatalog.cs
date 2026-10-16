using Unity.Collections;
using UnityEngine;
using Shooter.Infrastructure;

namespace Shooter.Game
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
