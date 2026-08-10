using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Skin Catalog", fileName = "SkinCatalog")]
    public class SkinCatalog : Catalog<SkinSpec>
    {
    }
}
