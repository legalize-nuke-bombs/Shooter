using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Tip Catalog", fileName = "TipCatalog")]
    public sealed class TipCatalog : Catalog<TipAsset>
    {
    }
}
