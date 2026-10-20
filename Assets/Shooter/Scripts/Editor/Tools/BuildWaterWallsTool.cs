using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Shooter.Game;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Editor
{
    public static class BuildWaterWallsTool
    {
        private const string WallsObjectName = "Walls";
        private const string WallsLayerName = "Ignore Raycast";
        private const float GridStep = 1f;
        private const float DryHeightWhereNoGround = 1000f;
        private const float SunkBelowDeepLine = 1f;
        private const float ShortestPiece = 0.01f;
        private static readonly Journal Log = Logs.Here();

        [MenuItem("Tools/Build Water Walls")]
        private static void BuildWallsForEveryWater()
        {
            if (EditorApplication.isPlaying)
            {
                Log.Warn("Water walls are built in edit mode only, stop the play mode first");
                return;
            }

            Water[] waters = Object.FindObjectsByType<Water>(FindObjectsInactive.Include);
            if (waters.Length == 0)
            {
                Log.Warn("No Water in the scene, nothing to build");
                return;
            }

            foreach (Water water in waters)
            {
                BuildWallsForWater(water);
            }
        }

        private static void BuildWallsForWater(Water water)
        {
            string scenePath = water.gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                Log.Warn($"Water {water.name} lives in a scene that was never saved, its walls have nowhere to be stored");
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            Transform waterTransform = water.transform;
            float waterLevel = waterTransform.position.y;
            float deepLineHeight = waterLevel - water.WallDepth;
            Vector3 extent = waterTransform.lossyScale / 2f;
            Vector2 gridOrigin = new(waterTransform.position.x - extent.x, waterTransform.position.z - extent.z);

            float[,] groundHeights = SampleGroundHeightsWithOuterRing(gridOrigin, extent, deepLineHeight);
            Transform wallsTransform = FindOrCreateUnscaledWallsUnder(waterTransform);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float wallLength = AddRibbonByMarchingSquares(groundHeights, gridOrigin, deepLineHeight, deepLineHeight - SunkBelowDeepLine, waterLevel + water.WallHeight, wallsTransform, vertices, triangles);

            string meshPath = $"{Path.GetDirectoryName(scenePath)}/WaterWalls-{water.name}.asset";

            if (triangles.Count == 0)
            {
                Object.DestroyImmediate(wallsTransform.gameObject);
                AssetDatabase.DeleteAsset(meshPath);
                EditorSceneManager.MarkSceneDirty(water.gameObject.scene);

                Log.Info($"Water {water.name} is nowhere deeper than {water.WallDepth} m, it needs no walls");
                return;
            }

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            mesh.Clear();
            mesh.name = $"WaterWalls-{water.name}";
            mesh.indexFormat = vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);

            MeshCollider collider = wallsTransform.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = wallsTransform.gameObject.AddComponent<MeshCollider>();
            }

            RecookCollider(collider, mesh);
            EditorSceneManager.MarkSceneDirty(water.gameObject.scene);

            Log.Info($"Water {water.name}: {wallLength:F0} m of wall in {triangles.Count / 6} quads where it gets {water.WallDepth} m deep, saved to {meshPath} in {stopwatch.ElapsedMilliseconds} ms");
        }

        private static void RecookCollider(MeshCollider collider, Mesh mesh)
        {
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }

        private static float[,] SampleGroundHeightsWithOuterRing(Vector2 gridOrigin, Vector3 extent, float deepLineHeight)
        {
            int nodesX = Mathf.FloorToInt(extent.x * 2f / GridStep) + 1;
            int nodesZ = Mathf.FloorToInt(extent.z * 2f / GridStep) + 1;

            float[,] groundHeights = new float[nodesX + 2, nodesZ + 2];
            for (int x = 0; x < nodesX + 2; x++)
            {
                for (int z = 0; z < nodesZ + 2; z++)
                {
                    groundHeights[x, z] = float.NegativeInfinity;
                }
            }

            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                TerrainData terrainData = terrain.terrainData;
                Vector3 terrainCorner = terrain.GetPosition();

                int fromX = Mathf.Max(0, Mathf.CeilToInt((terrainCorner.x - gridOrigin.x) / GridStep));
                int fromZ = Mathf.Max(0, Mathf.CeilToInt((terrainCorner.z - gridOrigin.y) / GridStep));
                int toX = Mathf.Min(nodesX - 1, Mathf.FloorToInt((terrainCorner.x + terrainData.size.x - gridOrigin.x) / GridStep));
                int toZ = Mathf.Min(nodesZ - 1, Mathf.FloorToInt((terrainCorner.z + terrainData.size.z - gridOrigin.y) / GridStep));
                if (fromX > toX || fromZ > toZ) continue;

                float fromU = (gridOrigin.x + fromX * GridStep - terrainCorner.x) / terrainData.size.x;
                float fromV = (gridOrigin.y + fromZ * GridStep - terrainCorner.z) / terrainData.size.z;
                float[,] heights = terrainData.GetInterpolatedHeights(fromU, fromV, toX - fromX + 1, toZ - fromZ + 1, GridStep / terrainData.size.x, GridStep / terrainData.size.z);

                for (int x = fromX; x <= toX; x++)
                {
                    for (int z = fromZ; z <= toZ; z++)
                    {
                        groundHeights[x + 1, z + 1] = Mathf.Max(groundHeights[x + 1, z + 1], terrainCorner.y + heights[z - fromZ, x - fromX]);
                    }
                }
            }

            for (int x = 0; x < nodesX + 2; x++)
            {
                for (int z = 0; z < nodesZ + 2; z++)
                {
                    if (float.IsNegativeInfinity(groundHeights[x, z]))
                    {
                        groundHeights[x, z] = deepLineHeight + DryHeightWhereNoGround;
                    }
                }
            }

            return groundHeights;
        }

        private static Transform FindOrCreateUnscaledWallsUnder(Transform waterTransform)
        {
            Transform wallsTransform = waterTransform.Find(WallsObjectName);
            if (wallsTransform == null)
            {
                wallsTransform = new GameObject(WallsObjectName).transform;
                wallsTransform.SetParent(waterTransform, false);
            }

            Vector3 waterScale = waterTransform.lossyScale;
            wallsTransform.localPosition = Vector3.zero;
            wallsTransform.localRotation = Quaternion.identity;
            wallsTransform.localScale = new Vector3(1f / waterScale.x, 1f / waterScale.y, 1f / waterScale.z);
            wallsTransform.gameObject.layer = LayerMask.NameToLayer(WallsLayerName);

            return wallsTransform;
        }

        private static float AddRibbonByMarchingSquares(float[,] groundHeights, Vector2 gridOrigin, float deepLineHeight, float wallBottom, float wallTop, Transform wallsTransform, List<Vector3> vertices, List<int> triangles)
        {
            float ribbonLength = 0f;
            int cellsX = groundHeights.GetLength(0) - 1;
            int cellsZ = groundHeights.GetLength(1) - 1;
            Vector2 framedOrigin = gridOrigin - new Vector2(GridStep, GridStep);

            for (int x = 0; x < cellsX; x++)
            {
                for (int z = 0; z < cellsZ; z++)
                {
                    Cell cell = new()
                    {
                        Origin = framedOrigin + new Vector2(x, z) * GridStep,
                        NearLeftHeight = groundHeights[x, z],
                        NearRightHeight = groundHeights[x + 1, z],
                        FarRightHeight = groundHeights[x + 1, z + 1],
                        FarLeftHeight = groundHeights[x, z + 1],
                        DeepLineHeight = deepLineHeight
                    };

                    int deepCornerMask = cell.GetDeepCornerMask();
                    if (deepCornerMask == 0 || deepCornerMask == 15) continue;

                    bool isSaddle = deepCornerMask == 5 || deepCornerMask == 10;
                    if (!isSaddle)
                    {
                        int fromEdge = -1;
                        int toEdge = -1;
                        for (int edge = 0; edge < 4; edge++)
                        {
                            if (!cell.IsEdgeCrossedByDeepLine(edge)) continue;

                            if (fromEdge < 0)
                            {
                                fromEdge = edge;
                            }
                            else
                            {
                                toEdge = edge;
                            }
                        }

                        int witnessCorner = 0;
                        while (!cell.IsDeep(witnessCorner))
                        {
                            witnessCorner++;
                        }

                        ribbonLength += AddQuadFacingShore(cell, fromEdge, toEdge, witnessCorner, wallBottom, wallTop, wallsTransform, vertices, triangles);
                        continue;
                    }

                    bool isMiddleDeep = (cell.NearLeftHeight + cell.NearRightHeight + cell.FarRightHeight + cell.FarLeftHeight) / 4f < deepLineHeight;
                    bool isNearLeftCornerCutOff = deepCornerMask == 5 ? !isMiddleDeep : isMiddleDeep;

                    if (isNearLeftCornerCutOff)
                    {
                        ribbonLength += AddQuadFacingShore(cell, Cell.NearEdge, Cell.LeftEdge, Cell.NearLeftCorner, wallBottom, wallTop, wallsTransform, vertices, triangles);
                        ribbonLength += AddQuadFacingShore(cell, Cell.RightEdge, Cell.FarEdge, Cell.FarRightCorner, wallBottom, wallTop, wallsTransform, vertices, triangles);
                    }
                    else
                    {
                        ribbonLength += AddQuadFacingShore(cell, Cell.NearEdge, Cell.RightEdge, Cell.NearRightCorner, wallBottom, wallTop, wallsTransform, vertices, triangles);
                        ribbonLength += AddQuadFacingShore(cell, Cell.FarEdge, Cell.LeftEdge, Cell.FarLeftCorner, wallBottom, wallTop, wallsTransform, vertices, triangles);
                    }
                }
            }

            return ribbonLength;
        }

        private static float AddQuadFacingShore(Cell cell, int fromEdge, int toEdge, int witnessCorner, float wallBottom, float wallTop, Transform wallsTransform, List<Vector3> vertices, List<int> triangles)
        {
            Vector2 fromCrossing = cell.GetDeepLineCrossing(fromEdge);
            Vector2 toCrossing = cell.GetDeepLineCrossing(toEdge);

            Vector2 along = toCrossing - fromCrossing;
            float length = along.magnitude;
            if (length < ShortestPiece) return 0f;

            Vector2 aside = cell.GetCornerPosition(witnessCorner) - fromCrossing;
            bool isWitnessOnLeft = along.x * aside.y - along.y * aside.x > 0f;
            bool isDeepOnLeft = isWitnessOnLeft == cell.IsDeep(witnessCorner);
            if (!isDeepOnLeft)
            {
                (fromCrossing, toCrossing) = (toCrossing, fromCrossing);
            }

            int firstVertex = vertices.Count;
            vertices.Add(wallsTransform.InverseTransformPoint(new Vector3(fromCrossing.x, wallBottom, fromCrossing.y)));
            vertices.Add(wallsTransform.InverseTransformPoint(new Vector3(fromCrossing.x, wallTop, fromCrossing.y)));
            vertices.Add(wallsTransform.InverseTransformPoint(new Vector3(toCrossing.x, wallBottom, toCrossing.y)));
            vertices.Add(wallsTransform.InverseTransformPoint(new Vector3(toCrossing.x, wallTop, toCrossing.y)));

            triangles.Add(firstVertex);
            triangles.Add(firstVertex + 1);
            triangles.Add(firstVertex + 2);
            triangles.Add(firstVertex + 1);
            triangles.Add(firstVertex + 3);
            triangles.Add(firstVertex + 2);

            return length;
        }

        private struct Cell
        {
            public const int NearEdge = 0;
            public const int RightEdge = 1;
            public const int FarEdge = 2;
            public const int LeftEdge = 3;

            public const int NearLeftCorner = 0;
            public const int NearRightCorner = 1;
            public const int FarRightCorner = 2;
            public const int FarLeftCorner = 3;

            public Vector2 Origin;
            public float NearLeftHeight;
            public float NearRightHeight;
            public float FarRightHeight;
            public float FarLeftHeight;
            public float DeepLineHeight;

            public int GetDeepCornerMask()
            {
                int mask = 0;
                for (int corner = 0; corner < 4; corner++)
                {
                    if (IsDeep(corner))
                    {
                        mask |= 1 << corner;
                    }
                }

                return mask;
            }

            public bool IsDeep(int corner)
            {
                return GetGroundHeight(corner) < DeepLineHeight;
            }

            public Vector2 GetCornerPosition(int corner)
            {
                return corner switch
                {
                    NearLeftCorner => Origin,
                    NearRightCorner => Origin + new Vector2(GridStep, 0f),
                    FarRightCorner => Origin + new Vector2(GridStep, GridStep),
                    _ => Origin + new Vector2(0f, GridStep)
                };
            }

            public bool IsEdgeCrossedByDeepLine(int edge)
            {
                (int fromCorner, int toCorner) = GetEdgeCornersRightOrAway(edge);
                return IsDeep(fromCorner) != IsDeep(toCorner);
            }

            public Vector2 GetDeepLineCrossing(int edge)
            {
                (int fromCorner, int toCorner) = GetEdgeCornersRightOrAway(edge);
                float along = (DeepLineHeight - GetGroundHeight(fromCorner)) / (GetGroundHeight(toCorner) - GetGroundHeight(fromCorner));

                return Vector2.LerpUnclamped(GetCornerPosition(fromCorner), GetCornerPosition(toCorner), along);
            }

            private float GetGroundHeight(int corner)
            {
                return corner switch
                {
                    NearLeftCorner => NearLeftHeight,
                    NearRightCorner => NearRightHeight,
                    FarRightCorner => FarRightHeight,
                    _ => FarLeftHeight
                };
            }

            private static (int fromCorner, int toCorner) GetEdgeCornersRightOrAway(int edge)
            {
                return edge switch
                {
                    NearEdge => (NearLeftCorner, NearRightCorner),
                    RightEdge => (NearRightCorner, FarRightCorner),
                    FarEdge => (FarLeftCorner, FarRightCorner),
                    _ => (NearLeftCorner, FarLeftCorner)
                };
            }
        }
    }
}
