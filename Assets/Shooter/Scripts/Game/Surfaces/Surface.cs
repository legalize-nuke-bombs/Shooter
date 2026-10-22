using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public static class Surface
    {
        private const float RayStartAboveFeet = 2f;
        private const float RayReachBelowFeet = 1f;
        private const float TreeHitAboveGround = 0.05f;

        private static int groundMask;

        private static int GroundMask => groundMask != 0 ? groundMask : groundMask = LayerMask.GetMask("Default", "Water");

        public static PhysicsMaterial FindUnder(CharacterController characterController)
        {
            return FindUnder(characterController.transform.position + characterController.center + Vector3.down * (characterController.height / 2f));
        }

        public static PhysicsMaterial FindUnder(Vector3 feetPosition)
        {
            Vector3 rayStart = feetPosition + Vector3.up * RayStartAboveFeet;
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, RayStartAboveFeet + RayReachBelowFeet, GroundMask, QueryTriggerInteraction.Ignore)) return null;

            return FindAt(hit);
        }

        public static PhysicsMaterial FindAt(RaycastHit hit)
        {
            return FindAt(hit.collider, hit.point);
        }

        public static PhysicsMaterial FindAt(Collider collider, Vector3 point)
        {
            if (collider is not TerrainCollider) return collider.sharedMaterial;

            Terrain terrain = collider.GetComponent<Terrain>();
            if (terrain == null) return null;
            if (point.y > terrain.SampleHeight(point) + terrain.GetPosition().y + TreeHitAboveGround) return FindTreeSurface(terrain, point);

            SurfaceCatalog surfaceCatalog = Catalogs.GetCatalog<SurfaceCatalog>();
            return surfaceCatalog == null ? null : surfaceCatalog.GetSurfaceOf(FindPrevailingTerrainLayer(terrain, point));
        }

        private static PhysicsMaterial FindTreeSurface(Terrain terrain, Vector3 point)
        {
            TerrainData terrainData = terrain.terrainData;
            int prototypeIndex = FindNearestTreePrototypeIndex(terrain, point);
            if (prototypeIndex < 0) return null;

            GameObject treePrefab = terrainData.treePrototypes[prototypeIndex].prefab;
            if (treePrefab == null) return null;

            foreach (Collider treeCollider in treePrefab.GetComponentsInChildren<Collider>(true))
            {
                if (treeCollider.sharedMaterial != null) return treeCollider.sharedMaterial;
            }

            return null;
        }

        private static int FindNearestTreePrototypeIndex(Terrain terrain, Vector3 point)
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainSize = terrainData.size;
            Vector3 localPoint = point - terrain.GetPosition();
            int nearestPrototypeIndex = -1;
            float nearestDistanceSquared = float.PositiveInfinity;

            for (int treeIndex = 0; treeIndex < terrainData.treeInstanceCount; treeIndex++)
            {
                TreeInstance tree = terrainData.GetTreeInstance(treeIndex);
                Vector3 treePosition = Vector3.Scale(tree.position, terrainSize);
                float distanceSquared = new Vector2(treePosition.x - localPoint.x, treePosition.z - localPoint.z).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared) continue;

                nearestPrototypeIndex = tree.prototypeIndex;
                nearestDistanceSquared = distanceSquared;
            }

            return nearestPrototypeIndex;
        }

        public static bool IsWater(PhysicsMaterial surface)
        {
            SurfaceCatalog surfaceCatalog = Catalogs.GetCatalog<SurfaceCatalog>();
            return surface != null && surfaceCatalog != null && surface == surfaceCatalog.Water;
        }

        private static TerrainLayer FindPrevailingTerrainLayer(Terrain terrain, Vector3 point)
        {
            TerrainData terrainData = terrain.terrainData;
            TerrainLayer[] terrainLayers = terrainData.terrainLayers;
            if (terrainLayers.Length == 0) return null;

            Vector3 localPoint = point - terrain.GetPosition();
            int alphamapX = Mathf.Clamp((int)(localPoint.x / terrainData.size.x * terrainData.alphamapWidth), 0, terrainData.alphamapWidth - 1);
            int alphamapZ = Mathf.Clamp((int)(localPoint.z / terrainData.size.z * terrainData.alphamapHeight), 0, terrainData.alphamapHeight - 1);

            float[,,] layerWeights = terrainData.GetAlphamaps(alphamapX, alphamapZ, 1, 1);
            int prevailingLayerIndex = 0;

            for (int layerIndex = 1; layerIndex < terrainLayers.Length; layerIndex++)
            {
                if (layerWeights[0, 0, layerIndex] > layerWeights[0, 0, prevailingLayerIndex]) prevailingLayerIndex = layerIndex;
            }

            return terrainLayers[prevailingLayerIndex];
        }
    }
}
