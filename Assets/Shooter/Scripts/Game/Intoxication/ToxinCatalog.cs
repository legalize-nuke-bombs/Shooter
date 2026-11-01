using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Intoxication/Toxin Catalog", fileName = "ToxinCatalog")]
    public class ToxinCatalog : Catalog<ToxinAsset>
    {
    }
}
