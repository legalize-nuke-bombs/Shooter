using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Surface Catalog", fileName = "SurfaceCatalog")]
    public class SurfaceCatalog : Catalog
    {
        [SerializeField] private PhysicsMaterial water;

        [SerializeField] private PhysicsMaterial flesh;

        [SerializeField] private TerrainSurface[] terrain;

        public PhysicsMaterial Water => water;

        public PhysicsMaterial Flesh => flesh;

        public PhysicsMaterial GetSurfaceOf(TerrainLayer layer)
        {
            if (layer == null || terrain == null) return null;

            foreach (TerrainSurface terrainSurface in terrain)
            {
                if (terrainSurface.Layer == layer) return terrainSurface.Surface;
            }

            return null;
        }
    }
}
