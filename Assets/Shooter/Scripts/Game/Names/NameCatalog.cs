using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Name Catalog", fileName = "NameCatalog")]
    public sealed class NameCatalog : Catalog<NameSpec>
    {
    }
}
