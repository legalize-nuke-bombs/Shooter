using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.World
{
    public static class Surface
    {
        private const float Lift = 0.5f;
        private const float Reach = 1f;

        private static int groundMask;

        private static int GroundMask => groundMask != 0
            ? groundMask
            : groundMask = LayerMask.GetMask("Default");

        public static PhysicsMaterial Under(CharacterController body)
        {
            return Under(body.transform.position + body.center + Vector3.down * (body.height / 2f));
        }

        public static PhysicsMaterial Under(Vector3 feet)
        {
            if (Water.Depth(feet) > 0f)
            {
                SurfaceCatalog catalog = Catalogs.Of<SurfaceCatalog>();
                return catalog == null ? null : catalog.Water;
            }

            return Physics.Raycast(feet + Vector3.up * Lift, Vector3.down, out RaycastHit hit, Lift + Reach,
                GroundMask, QueryTriggerInteraction.Ignore)
                ? Of(hit)
                : null;
        }

        public static PhysicsMaterial Of(RaycastHit hit)
        {
            if (hit.collider is not TerrainCollider) return hit.collider.sharedMaterial;

            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain == null) return null;

            SurfaceCatalog catalog = Catalogs.Of<SurfaceCatalog>();
            return catalog == null ? null : catalog.Of(Prevailing(terrain, hit.point));
        }

        private static TerrainLayer Prevailing(Terrain terrain, Vector3 point)
        {
            TerrainData data = terrain.terrainData;
            TerrainLayer[] layers = data.terrainLayers;
            if (layers.Length == 0) return null;

            Vector3 local = point - terrain.GetPosition();
            int x = Mathf.Clamp((int)(local.x / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
            int z = Mathf.Clamp((int)(local.z / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);

            float[,,] weights = data.GetAlphamaps(x, z, 1, 1);
            int prevailing = 0;

            for (int i = 1; i < layers.Length; i++)
                if (weights[0, 0, i] > weights[0, 0, prevailing])
                    prevailing = i;

            return layers[prevailing];
        }
    }
}
