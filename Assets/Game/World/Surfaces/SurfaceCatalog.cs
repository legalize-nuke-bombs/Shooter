using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.World
{
    [CreateAssetMenu(menuName = "Shooter/Surface Catalog", fileName = "SurfaceCatalog")]
    public class SurfaceCatalog : Catalog
    {
        [SerializeField] private PhysicsMaterial water;

        [SerializeField] private TerrainSurface[] terrain;

        public PhysicsMaterial Water => water;

        public PhysicsMaterial Of(TerrainLayer layer)
        {
            if (layer == null || terrain == null) return null;

            foreach (TerrainSurface row in terrain)
                if (row.Layer == layer)
                    return row.Surface;

            return null;
        }
    }
}
