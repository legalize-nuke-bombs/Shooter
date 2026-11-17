using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    // What Unity's lightmapper does for probe volumes before it lights them, done by physics instead:
    // a probe buried in a wall, a floor or the ground is marked invalid so the probe volume ignores it.
    // Only colliders of objects that contribute GI count: the probe volume does not see props, so a probe under a bed stays valid
    public static class StuckProbes
    {
        private const int RayCount = 16;
        private const float RayLength = 2f;
        private const float InsideColliderRadius = 0.02f;
        private const float StuckBackfaceShare = 0.25f;
        private const int MaxHitsPerQuery = 32;

        public static float[] FindValidity(NativeArray<Vector3> positions)
        {
            var validity = new float[positions.Length];
            Terrain[] terrains = Terrain.activeTerrains;
            Vector3[] directions = SpreadOverSphere(RayCount);
            var query = new SolidQuery(LayerMask.GetMask("Default", "Terrain"));
            bool hitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            Physics.SyncTransforms();
            try
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    if (!IsClean(positions[i], terrains, directions, query)) validity[i] = 1f;
                }
            }
            finally
            {
                Physics.queriesHitBackfaces = hitBackfaces;
            }

            return validity;
        }

        private static bool IsClean(Vector3 position, Terrain[] terrains, Vector3[] directions, SolidQuery query)
        {
            if (TryGetTerrainHeight(position, terrains, out float ground) && position.y < ground) return false;
            if (query.IsInside(position, InsideColliderRadius)) return false;

            int backfaceHits = 0;
            foreach (Vector3 direction in directions)
            {
                if (query.TryGetClosestHit(position, direction, RayLength, out RaycastHit hit) && Vector3.Dot(hit.normal, direction) > 0f)
                {
                    backfaceHits++;
                }
            }

            return backfaceHits <= StuckBackfaceShare * directions.Length;
        }

        // The lowest terrain surface over the point: a terrain patch lying above the map's ground does not bury what is under it
        private static bool TryGetTerrainHeight(Vector3 position, Terrain[] terrains, out float height)
        {
            height = float.PositiveInfinity;
            foreach (Terrain terrain in terrains)
            {
                Vector3 origin = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (position.x < origin.x || position.x > origin.x + size.x || position.z < origin.z || position.z > origin.z + size.z) continue;

                height = Mathf.Min(height, origin.y + terrain.SampleHeight(position));
            }

            return !float.IsPositiveInfinity(height);
        }

        private static Vector3[] SpreadOverSphere(int count)
        {
            var directions = new Vector3[count];
            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int i = 0; i < count; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / count;
                float radius = Mathf.Sqrt(1f - y * y);
                float angle = goldenAngle * i;
                directions[i] = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
            }

            return directions;
        }

        private class SolidQuery
        {
            private readonly int layers;
            private readonly Collider[] overlaps = new Collider[MaxHitsPerQuery];
            private readonly RaycastHit[] hits = new RaycastHit[MaxHitsPerQuery];
            private readonly Dictionary<Collider, bool> contributesByCollider = new();

            public SolidQuery(int layers)
            {
                this.layers = layers;
            }

            public bool IsInside(Vector3 position, float radius)
            {
                int count = Physics.OverlapSphereNonAlloc(position, radius, overlaps, layers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    if (ContributesGlobalIllumination(overlaps[i])) return true;
                }

                return false;
            }

            public bool TryGetClosestHit(Vector3 origin, Vector3 direction, float length, out RaycastHit closest)
            {
                closest = default;
                bool found = false;
                int count = Physics.RaycastNonAlloc(origin, direction, hits, length, layers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    if ((found && hits[i].distance >= closest.distance) || !ContributesGlobalIllumination(hits[i].collider)) continue;

                    closest = hits[i];
                    found = true;
                }

                return found;
            }

            private bool ContributesGlobalIllumination(Collider collider)
            {
                if (!contributesByCollider.TryGetValue(collider, out bool contributes))
                {
                    contributes = GameObjectUtility.GetStaticEditorFlags(collider.gameObject).HasFlag(StaticEditorFlags.ContributeGI);
                    contributesByCollider[collider] = contributes;
                }

                return contributes;
            }
        }
    }
}
