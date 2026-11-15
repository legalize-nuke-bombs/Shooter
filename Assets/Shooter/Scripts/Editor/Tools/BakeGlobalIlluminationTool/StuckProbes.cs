using Unity.Collections;
using UnityEngine;

namespace Shooter.Editor
{
    // What Unity's lightmapper does for probe volumes before it lights them, done by physics instead:
    // a probe buried in a wall, a floor or the ground is moved to the nearest clean spot within the shift limit,
    // and the ones that cannot get out are marked invalid so the probe volume ignores them.
    public static class StuckProbes
    {
        private const int RayCount = 32;
        private const int CheckRayCount = 16;
        private const float RayLength = 2f;
        private const float InsideColliderRadius = 0.02f;
        private const float StuckBackfaceShare = 0.25f;
        private const float ClearanceBeyondSurface = 0.05f;
        private const int ExitsToTry = 4;

        // The sky step runs before the lighting step in a bake and leaves the validity it found here for it
        private static float[] lastValidity;

        public static float[] TakeLastValidity()
        {
            float[] validity = lastValidity;
            lastValidity = null;
            return validity;
        }

        public static Vector3[] PushOut(NativeArray<Vector3> positions, float maxShift, out int pushed, out int stuck)
        {
            var result = new Vector3[positions.Length];
            var validity = new float[positions.Length];
            pushed = 0;
            stuck = 0;

            Terrain[] terrains = Terrain.activeTerrains;
            Vector3[] directions = SpreadOverSphere(RayCount);
            Vector3[] checkDirections = SpreadOverSphere(CheckRayCount);
            int solidLayers = LayerMask.GetMask("Default");
            bool hitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            Physics.SyncTransforms();
            try
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector3 position = positions[i];
                    result[i] = position;
                    if (IsClean(position, terrains, checkDirections, solidLayers)) continue;

                    if (TryFindExit(position, maxShift, terrains, directions, checkDirections, solidLayers, out Vector3 exit))
                    {
                        result[i] = exit;
                        pushed++;
                    }
                    else
                    {
                        validity[i] = 1f;
                        stuck++;
                    }
                }
            }
            finally
            {
                Physics.queriesHitBackfaces = hitBackfaces;
            }

            lastValidity = validity;
            return result;
        }

        private static bool TryFindExit(Vector3 position, float maxShift, Terrain[] terrains, Vector3[] directions, Vector3[] checkDirections, int solidLayers, out Vector3 exit)
        {
            if (TryGetTerrainHeight(position, terrains, out float ground) && position.y < ground)
            {
                exit = new Vector3(position.x, ground + ClearanceBeyondSurface, position.z);
                return ground - position.y <= maxShift && IsClean(exit, terrains, checkDirections, solidLayers);
            }

            var exits = new (float distance, Vector3 direction)[directions.Length];
            int exitCount = 0;
            foreach (Vector3 direction in directions)
            {
                if (Physics.Raycast(position, direction, out RaycastHit hit, maxShift, solidLayers, QueryTriggerInteraction.Ignore)
                    && Vector3.Dot(hit.normal, direction) > 0f)
                {
                    exits[exitCount++] = (hit.distance, direction);
                }
            }

            System.Array.Sort(exits, 0, exitCount, Comparer.Instance);
            for (int i = 0; i < exitCount && i < ExitsToTry; i++)
            {
                Vector3 candidate = position + exits[i].direction * (exits[i].distance + ClearanceBeyondSurface);
                if (IsClean(candidate, terrains, checkDirections, solidLayers))
                {
                    exit = candidate;
                    return true;
                }
            }

            for (float shift = maxShift * 0.25f; shift <= maxShift + 0.001f; shift += maxShift * 0.25f)
            {
                foreach (Vector3 direction in directions)
                {
                    Vector3 candidate = position + direction * shift;
                    if (IsClean(candidate, terrains, checkDirections, solidLayers))
                    {
                        exit = candidate;
                        return true;
                    }
                }
            }

            exit = position;
            return false;
        }

        private static bool IsClean(Vector3 position, Terrain[] terrains, Vector3[] directions, int solidLayers)
        {
            if (TryGetTerrainHeight(position, terrains, out float ground) && position.y < ground) return false;
            if (Physics.CheckSphere(position, InsideColliderRadius, solidLayers, QueryTriggerInteraction.Ignore)) return false;

            int backfaceHits = 0;
            foreach (Vector3 direction in directions)
            {
                if (Physics.Raycast(position, direction, out RaycastHit hit, RayLength, solidLayers, QueryTriggerInteraction.Ignore)
                    && Vector3.Dot(hit.normal, direction) > 0f)
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

        private class Comparer : System.Collections.Generic.IComparer<(float distance, Vector3 direction)>
        {
            public static readonly Comparer Instance = new();

            public int Compare((float distance, Vector3 direction) a, (float distance, Vector3 direction) b)
            {
                return a.distance.CompareTo(b.distance);
            }
        }
    }
}
