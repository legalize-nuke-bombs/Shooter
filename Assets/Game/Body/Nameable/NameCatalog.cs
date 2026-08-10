using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Name Catalog", fileName = "NameCatalog")]
    public sealed class NameCatalog : Catalog<NameSpec>
    {
    }
}
