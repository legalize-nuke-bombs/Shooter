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
        private const string WallsName = "Walls";
        private const string WallsLayer = "Ignore Raycast";
        private const float GridStep = 1f;
        private const float DryHeightWhereNoGround = 1000f;
        private const float SunkBelowLine = 1f;
        private const float ShortestPiece = 0.01f;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [MenuItem("Tools/Build Water Walls")]
        private static void BuildForEveryWater()
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
                BuildFor(water);
            }
        }

        private static void BuildFor(Water water)
        {
            string scenePath = water.gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                Log.Warn($"Water {water.name} lives in a scene that was never saved, its walls have nowhere to be stored");
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            Transform surface = water.transform;
            float level = surface.position.y;
            float line = level - water.WallDepth;
            Vector3 extent = surface.lossyScale / 2f;
            Vector2 origin = new(surface.position.x - extent.x, surface.position.z - extent.z);

            float[,] ground = GroundHeightsWithOuterRing(origin, extent, line);
            Transform walls = UnscaledWallsUnder(surface);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float length = AddRibbonByMarchingSquares(ground, origin, line, line - SunkBelowLine, level + water.WallHeight, walls, vertices, triangles);

            string path = $"{Path.GetDirectoryName(scenePath)}/WaterWalls-{water.name}.asset";

            if (triangles.Count == 0)
            {
                Object.DestroyImmediate(walls.gameObject);
                AssetDatabase.DeleteAsset(path);
                EditorSceneManager.MarkSceneDirty(water.gameObject.scene);

                Log.Info($"Water {water.name} is nowhere deeper than {water.WallDepth} m, it needs no walls");
                return;
            }

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.name = $"WaterWalls-{water.name}";
            mesh.indexFormat = vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);

            MeshCollider collider = walls.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = walls.gameObject.AddComponent<MeshCollider>();
            }

            Recook(collider, mesh);
            EditorSceneManager.MarkSceneDirty(water.gameObject.scene);

            Log.Info($"Water {water.name}: {length:F0} m of wall in {triangles.Count / 6} quads where it gets {water.WallDepth} m deep, saved to {path} in {stopwatch.ElapsedMilliseconds} ms");
        }

        private static void Recook(MeshCollider collider, Mesh mesh)
        {
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }

        private static float[,] GroundHeightsWithOuterRing(Vector2 origin, Vector3 extent, float line)
        {
            int nodesX = Mathf.FloorToInt(extent.x * 2f / GridStep) + 1;
            int nodesZ = Mathf.FloorToInt(extent.z * 2f / GridStep) + 1;

            float[,] ground = new float[nodesX + 2, nodesZ + 2];
            for (int x = 0; x < nodesX + 2; x++)
            {
                for (int z = 0; z < nodesZ + 2; z++)
                {
                    ground[x, z] = float.NegativeInfinity;
                }
            }

            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                TerrainData data = terrain.terrainData;
                Vector3 corner = terrain.GetPosition();

                int fromX = Mathf.Max(0, Mathf.CeilToInt((corner.x - origin.x) / GridStep));
                int fromZ = Mathf.Max(0, Mathf.CeilToInt((corner.z - origin.y) / GridStep));
                int toX = Mathf.Min(nodesX - 1, Mathf.FloorToInt((corner.x + data.size.x - origin.x) / GridStep));
                int toZ = Mathf.Min(nodesZ - 1, Mathf.FloorToInt((corner.z + data.size.z - origin.y) / GridStep));
                if (fromX > toX || fromZ > toZ) continue;

                float fromU = (origin.x + fromX * GridStep - corner.x) / data.size.x;
                float fromV = (origin.y + fromZ * GridStep - corner.z) / data.size.z;
                float[,] heights = data.GetInterpolatedHeights(fromU, fromV, toX - fromX + 1, toZ - fromZ + 1, GridStep / data.size.x, GridStep / data.size.z);

                for (int x = fromX; x <= toX; x++)
                {
                    for (int z = fromZ; z <= toZ; z++)
                    {
                        ground[x + 1, z + 1] = Mathf.Max(ground[x + 1, z + 1], corner.y + heights[z - fromZ, x - fromX]);
                    }
                }
            }

            for (int x = 0; x < nodesX + 2; x++)
            {
                for (int z = 0; z < nodesZ + 2; z++)
                {
                    if (float.IsNegativeInfinity(ground[x, z]))
                    {
                        ground[x, z] = line + DryHeightWhereNoGround;
                    }
                }
            }

            return ground;
        }

        private static Transform UnscaledWallsUnder(Transform surface)
        {
            Transform walls = surface.Find(WallsName);
            if (walls == null)
            {
                walls = new GameObject(WallsName).transform;
                walls.SetParent(surface, false);
            }

            Vector3 scale = surface.lossyScale;
            walls.localPosition = Vector3.zero;
            walls.localRotation = Quaternion.identity;
            walls.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            walls.gameObject.layer = LayerMask.NameToLayer(WallsLayer);

            return walls;
        }

        private static float AddRibbonByMarchingSquares(float[,] ground, Vector2 origin, float line, float bottom, float top, Transform walls, List<Vector3> vertices, List<int> triangles)
        {
            float length = 0f;
            int cellsX = ground.GetLength(0) - 1;
            int cellsZ = ground.GetLength(1) - 1;
            Vector2 framedOrigin = origin - new Vector2(GridStep, GridStep);

            for (int x = 0; x < cellsX; x++)
            {
                for (int z = 0; z < cellsZ; z++)
                {
                    Cell cell = new()
                    {
                        Origin = framedOrigin + new Vector2(x, z) * GridStep,
                        NearLeft = ground[x, z],
                        NearRight = ground[x + 1, z],
                        FarRight = ground[x + 1, z + 1],
                        FarLeft = ground[x, z + 1],
                        Line = line
                    };

                    int deep = cell.DeepCorners();
                    if (deep == 0 || deep == 15) continue;

                    bool saddle = deep == 5 || deep == 10;
                    if (!saddle)
                    {
                        int from = -1;
                        int to = -1;
                        for (int edge = 0; edge < 4; edge++)
                        {
                            if (!cell.Crosses(edge)) continue;

                            if (from < 0)
                            {
                                from = edge;
                            }
                            else
                            {
                                to = edge;
                            }
                        }

                        int witness = 0;
                        while (!cell.Deep(witness))
                        {
                            witness++;
                        }

                        length += AddQuadFacingShore(cell, from, to, witness, bottom, top, walls, vertices, triangles);
                        continue;
                    }

                    bool middleDeep = (cell.NearLeft + cell.NearRight + cell.FarRight + cell.FarLeft) / 4f < line;
                    bool nearLeftCut = deep == 5 ? !middleDeep : middleDeep;

                    if (nearLeftCut)
                    {
                        length += AddQuadFacingShore(cell, Cell.Near, Cell.Left, Cell.NearLeftCorner, bottom, top, walls, vertices, triangles);
                        length += AddQuadFacingShore(cell, Cell.Right, Cell.Far, Cell.FarRightCorner, bottom, top, walls, vertices, triangles);
                    }
                    else
                    {
                        length += AddQuadFacingShore(cell, Cell.Near, Cell.Right, Cell.NearRightCorner, bottom, top, walls, vertices, triangles);
                        length += AddQuadFacingShore(cell, Cell.Far, Cell.Left, Cell.FarLeftCorner, bottom, top, walls, vertices, triangles);
                    }
                }
            }

            return length;
        }

        private static float AddQuadFacingShore(Cell cell, int fromEdge, int toEdge, int witness, float bottom, float top, Transform walls, List<Vector3> vertices, List<int> triangles)
        {
            Vector2 from = cell.LineCrossing(fromEdge);
            Vector2 to = cell.LineCrossing(toEdge);

            Vector2 along = to - from;
            float length = along.magnitude;
            if (length < ShortestPiece) return 0f;

            Vector2 aside = cell.Position(witness) - from;
            bool witnessOnLeft = along.x * aside.y - along.y * aside.x > 0f;
            bool deepOnLeft = witnessOnLeft == cell.Deep(witness);
            if (!deepOnLeft)
            {
                (from, to) = (to, from);
            }

            int first = vertices.Count;
            vertices.Add(walls.InverseTransformPoint(new Vector3(from.x, bottom, from.y)));
            vertices.Add(walls.InverseTransformPoint(new Vector3(from.x, top, from.y)));
            vertices.Add(walls.InverseTransformPoint(new Vector3(to.x, bottom, to.y)));
            vertices.Add(walls.InverseTransformPoint(new Vector3(to.x, top, to.y)));

            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first + 1);
            triangles.Add(first + 3);
            triangles.Add(first + 2);

            return length;
        }

        private struct Cell
        {
            public const int Near = 0;
            public const int Right = 1;
            public const int Far = 2;
            public const int Left = 3;

            public const int NearLeftCorner = 0;
            public const int NearRightCorner = 1;
            public const int FarRightCorner = 2;
            public const int FarLeftCorner = 3;

            public Vector2 Origin;
            public float NearLeft;
            public float NearRight;
            public float FarRight;
            public float FarLeft;
            public float Line;

            public int DeepCorners()
            {
                int mask = 0;
                for (int corner = 0; corner < 4; corner++)
                {
                    if (Deep(corner))
                    {
                        mask |= 1 << corner;
                    }
                }

                return mask;
            }

            public bool Deep(int corner)
            {
                return Ground(corner) < Line;
            }

            public Vector2 Position(int corner)
            {
                return corner switch
                {
                    NearLeftCorner => Origin,
                    NearRightCorner => Origin + new Vector2(GridStep, 0f),
                    FarRightCorner => Origin + new Vector2(GridStep, GridStep),
                    _ => Origin + new Vector2(0f, GridStep)
                };
            }

            public bool Crosses(int edge)
            {
                (int from, int to) = EndsRightOrAway(edge);
                return Deep(from) != Deep(to);
            }

            public Vector2 LineCrossing(int edge)
            {
                (int from, int to) = EndsRightOrAway(edge);
                float along = (Line - Ground(from)) / (Ground(to) - Ground(from));

                return Vector2.LerpUnclamped(Position(from), Position(to), along);
            }

            private float Ground(int corner)
            {
                return corner switch
                {
                    NearLeftCorner => NearLeft,
                    NearRightCorner => NearRight,
                    FarRightCorner => FarRight,
                    _ => FarLeft
                };
            }

            private static (int from, int to) EndsRightOrAway(int edge)
            {
                return edge switch
                {
                    Near => (NearLeftCorner, NearRightCorner),
                    Right => (NearRightCorner, FarRightCorner),
                    Far => (FarLeftCorner, FarRightCorner),
                    _ => (NearLeftCorner, FarLeftCorner)
                };
            }
        }
    }
}
