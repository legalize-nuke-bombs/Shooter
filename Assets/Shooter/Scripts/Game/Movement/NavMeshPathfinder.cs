using System.Collections.Generic;
using Unity.AI.Navigation.LowLevel;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Game
{
    public static class NavMeshPathfinder
    {
        private const int MaxPolygonVertices = 16;
        private const int MaxPolygonNeighbors = 64;
        private const float SamePointSqrDistance = 1f / 16384f / 16384f;
        private const float PortalTouchSqrDistance = 0.001f * 0.001f;

        private static readonly List<(Vector3 Left, Vector3 Right)> Portals = new();

        public static NavMeshPathStatus FindPath(Vector3 from, Vector3 to, int agentTypeId, int maxNodesToVisit, Vector3 locationExtents, List<Vector3> corners)
        {
            corners.Clear();

            NavWorld world = NavWorld.GetDefaultWorld();
            NavLocation start = world.MapLocation(from, locationExtents, agentTypeId);
            NavLocation end = world.MapLocation(to, locationExtents, agentTypeId);
            if (!world.IsValid(start) || !world.IsValid(end)) return NavMeshPathStatus.PathInvalid;

            NavQueryBuffer buffer = new(world, Allocator.Persistent, maxNodesToVisit);
            NativeArray<NavNode> polygons = default;
            try
            {
                world.BeginFindPath(buffer, start, end);
                world.ContinueFindPath(buffer, int.MaxValue, out _);
                world.EndFindPath(buffer, out int polygonCount);
                if (polygonCount == 0) return NavMeshPathStatus.PathInvalid;

                polygons = new NativeArray<NavNode>(polygonCount, Allocator.Temp);
                world.GetResultFromFindPath(buffer, polygons);

                Portals.Clear();
                for (int i = 0; i + 1 < polygonCount; i++)
                {
                    if (!world.GetPortalPoints(polygons[i], polygons[i + 1], out Vector3 left, out Vector3 right)) break;
                    Portals.Add((left, right));
                }

                NavNode lastPolygon = polygons[Portals.Count];
                bool complete = lastPolygon.Equals(end.node);
                Vector3 lastEntry = Portals.Count > 0 ? (Portals[^1].Left + Portals[^1].Right) * 0.5f : start.position;
                Vector3 pathEnd = complete ? end.position : ClosestPointOnPolygon(world, lastPolygon, to, lastEntry);

                PullString(start.position, pathEnd, corners);
                return complete ? NavMeshPathStatus.PathComplete : NavMeshPathStatus.PathPartial;
            }
            finally
            {
                buffer.Dispose();
                if (polygons.IsCreated) polygons.Dispose();
            }
        }

        // Detour's funnel from findStraightPath: it narrows through each portal, and once one side crosses the other,
        // the side that held becomes the next corner and the scan restarts from it
        private static void PullString(Vector3 start, Vector3 end, List<Vector3> corners)
        {
            Vector3 apex = start, left = start, right = start;
            int apexIndex = 0, leftIndex = 0, rightIndex = 0;

            for (int i = 0; i <= Portals.Count; i++)
            {
                (Vector3 nextLeft, Vector3 nextRight) = i < Portals.Count ? Portals[i] : (end, end);

                if (i == 0 && SqrDistanceToSegment2D(apex, nextLeft, nextRight) < PortalTouchSqrDistance) continue;

                if (TriangleArea2D(apex, right, nextRight) <= 0f)
                {
                    if (IsSamePoint(apex, right) || TriangleArea2D(apex, left, nextRight) > 0f)
                    {
                        right = nextRight;
                        rightIndex = i;
                    }
                    else
                    {
                        apex = left;
                        apexIndex = leftIndex;
                        AddCorner(corners, apex);
                        right = left = apex;
                        rightIndex = leftIndex = apexIndex;
                        i = apexIndex;
                        continue;
                    }
                }

                if (TriangleArea2D(apex, left, nextLeft) >= 0f)
                {
                    if (IsSamePoint(apex, left) || TriangleArea2D(apex, right, nextLeft) < 0f)
                    {
                        left = nextLeft;
                        leftIndex = i;
                    }
                    else
                    {
                        apex = right;
                        apexIndex = rightIndex;
                        AddCorner(corners, apex);
                        right = left = apex;
                        rightIndex = leftIndex = apexIndex;
                        i = apexIndex;
                    }
                }
            }

            AddCorner(corners, end);
        }

        private static Vector3 ClosestPointOnPolygon(NavWorld world, NavNode polygon, Vector3 point, Vector3 pointInPolygon)
        {
            NativeArray<Vector3> vertices = new(MaxPolygonVertices, Allocator.Temp);
            NativeArray<NavNode> neighbors = new(MaxPolygonNeighbors, Allocator.Temp);
            NativeArray<byte> edgeIndices = new(MaxPolygonNeighbors, Allocator.Temp);
            try
            {
                world.GetEdgesAndNeighbors(polygon, vertices, neighbors, edgeIndices, out int vertexCount, out _);
                if (vertexCount < 3) return pointInPolygon;

                Vector3 closest = pointInPolygon;
                float closestSqrDistance = float.MaxValue;
                bool anyLeftTurn = false, anyRightTurn = false;
                float heightSum = 0f;
                for (int i = 0; i < vertexCount; i++)
                {
                    Vector3 a = vertices[i];
                    Vector3 b = vertices[(i + 1) % vertexCount];
                    heightSum += a.y;

                    float turn = TriangleArea2D(a, b, point);
                    if (turn > 0f) anyLeftTurn = true;
                    if (turn < 0f) anyRightTurn = true;

                    Vector3 onEdge = Vector3.Lerp(a, b, SegmentParameter2D(point, a, b));
                    float sqrDistance = SqrDistance2D(point, onEdge);
                    if (sqrDistance < closestSqrDistance)
                    {
                        closestSqrDistance = sqrDistance;
                        closest = onEdge;
                    }
                }

                bool inside = !(anyLeftTurn && anyRightTurn);
                return inside ? new Vector3(point.x, heightSum / vertexCount, point.z) : closest;
            }
            finally
            {
                vertices.Dispose();
                neighbors.Dispose();
                edgeIndices.Dispose();
            }
        }

        private static void AddCorner(List<Vector3> corners, Vector3 corner)
        {
            if (corners.Count > 0 && IsSamePoint(corners[^1], corner)) return;

            corners.Add(corner);
        }

        private static float TriangleArea2D(Vector3 a, Vector3 b, Vector3 c)
        {
            return (c.x - a.x) * (b.z - a.z) - (b.x - a.x) * (c.z - a.z);
        }

        private static float SegmentParameter2D(Vector3 point, Vector3 a, Vector3 b)
        {
            float abx = b.x - a.x, abz = b.z - a.z;
            float lengthSqr = abx * abx + abz * abz;
            if (lengthSqr <= 0f) return 0f;

            return Mathf.Clamp01(((point.x - a.x) * abx + (point.z - a.z) * abz) / lengthSqr);
        }

        private static float SqrDistanceToSegment2D(Vector3 point, Vector3 a, Vector3 b)
        {
            return SqrDistance2D(point, Vector3.Lerp(a, b, SegmentParameter2D(point, a, b)));
        }

        private static float SqrDistance2D(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static bool IsSamePoint(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude < SamePointSqrDistance;
        }
    }
}
