using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Toxin Catalog", fileName = "ToxinCatalog")]
    public class ToxinCatalog : Catalog<ToxinSpec>
    {
    }
}
