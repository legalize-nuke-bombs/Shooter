using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Fraction Catalog", fileName = "FractionCatalog")]
    public class FractionCatalog : Catalog<Fraction>
    {
    }
}
