using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Client.Interface
{
    [CreateAssetMenu(menuName = "Shooter/Tip Catalog", fileName = "TipCatalog")]
    public sealed class TipCatalog : Catalog<TipSpec>
    {
    }
}
