using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Faction Catalog", fileName = "FactionCatalog")]
    public class FactionCatalog : Catalog<Faction>
    {
    }
}
