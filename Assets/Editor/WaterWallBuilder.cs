using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Editing
{
    // Nobody swims, so every Water gets an invisible wall along the line where it becomes Water.WallDepth deep.
    // Everything about the water comes from the Water object itself: the level is its height, the area is its scale.
    // The wall is a ribbon of upright quads facing the shore. Mesh colliders are one-sided, so the wall stops whoever
    // walks in from the shore and lets out whoever ended up behind it. Rebuild after the shore is reshaped, then bake the navmesh
    public static class WaterWallBuilder
    {
        private const string WallsName = "Walls";

        // Looks and bullets pass through, bodies do not
        private const string WallsLayer = "Ignore Raycast";

        // The wall follows the ground to within a step. It is invisible and stands waist-deep, a metre is plenty
        private const float Step = 1f;

        // Where there is no ground at all the node counts as dry land this far above the wall line,
        // so the wall closes along the edge of the map and of the water
        private const float Dry = 1000f;

        // The ribbon goes this far below the wall line, so no gap opens under it between the nodes
        private const float Sunk = 1f;

        private const float Shortest = 0.01f;
        private static readonly Journal Log = Logs.Here();

        [MenuItem("Tools/Build Water Walls")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Log.Error("Water walls are built in edit mode only, stop the play mode first");
                return;
            }

            Water[] waters = Object.FindObjectsByType<Water>(FindObjectsInactive.Include);
            if (waters.Length == 0)
            {
                Log.Error("No Water in the scene, nothing to build");
                return;
            }

            foreach (Water water in waters) Build(water);

            AssetDatabase.SaveAssets();
        }

        private static void Build(Water water)
        {
            string scenePath = water.gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                Log.Error($"Water {water.name} lives in a scene that was never saved, its walls have nowhere to be stored");
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            Transform surface = water.transform;
            float level = surface.position.y;
            float line = level - water.WallDepth;
            Vector3 extent = surface.lossyScale / 2f;
            var origin = new Vector2(surface.position.x - extent.x, surface.position.z - extent.z);

            float[,] ground = Ground(origin, extent, line);
            Transform walls = Walls(surface);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float length = Ribbon(ground, origin, line, line - Sunk, level + water.WallHeight, walls, vertices, triangles);

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

            MeshCollider collider = walls.GetComponent<MeshCollider>();
            if (collider == null) collider = walls.gameObject.AddComponent<MeshCollider>();

            // The same mesh asset is refilled in place, the collider has to take it anew to cook it again
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;

            EditorSceneManager.MarkSceneDirty(water.gameObject.scene);

            Log.Info(
                $"Water {water.name}: {length:F0} m of wall in {triangles.Count / 6} quads where it gets {water.WallDepth} m deep, saved to {path} in {stopwatch.ElapsedMilliseconds} ms");
        }

        // Height of the ground at every node of a grid over the water, framed by one ring of nodes outside of it
        private static float[,] Ground(Vector2 origin, Vector3 extent, float line)
        {
            int nodesX = Mathf.FloorToInt(extent.x * 2f / Step) + 1;
            int nodesZ = Mathf.FloorToInt(extent.z * 2f / Step) + 1;

            var ground = new float[nodesX + 2, nodesZ + 2];
            for (int x = 0; x < nodesX + 2; x++)
            for (int z = 0; z < nodesZ + 2; z++)
                ground[x, z] = float.NegativeInfinity;

            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                TerrainData data = terrain.terrainData;
                Vector3 corner = terrain.GetPosition();

                int fromX = Mathf.Max(0, Mathf.CeilToInt((corner.x - origin.x) / Step));
                int fromZ = Mathf.Max(0, Mathf.CeilToInt((corner.z - origin.y) / Step));
                int toX = Mathf.Min(nodesX - 1, Mathf.FloorToInt((corner.x + data.size.x - origin.x) / Step));
                int toZ = Mathf.Min(nodesZ - 1, Mathf.FloorToInt((corner.z + data.size.z - origin.y) / Step));
                if (fromX > toX || fromZ > toZ) continue;

                float[,] heights = data.GetInterpolatedHeights(
                    (origin.x + fromX * Step - corner.x) / data.size.x,
                    (origin.y + fromZ * Step - corner.z) / data.size.z,
                    toX - fromX + 1,
                    toZ - fromZ + 1,
                    Step / data.size.x,
                    Step / data.size.z);

                // Where terrains lie one above another, people walk on the upper one
                for (int x = fromX; x <= toX; x++)
                for (int z = fromZ; z <= toZ; z++)
                    ground[x + 1, z + 1] = Mathf.Max(ground[x + 1, z + 1], corner.y + heights[z - fromZ, x - fromX]);
            }

            for (int x = 0; x < nodesX + 2; x++)
            for (int z = 0; z < nodesZ + 2; z++)
                if (float.IsNegativeInfinity(ground[x, z]))
                    ground[x, z] = line + Dry;

            return ground;
        }

        private static Transform Walls(Transform surface)
        {
            Transform walls = surface.Find(WallsName);
            if (walls == null)
            {
                walls = new GameObject(WallsName).transform;
                walls.SetParent(surface, false);
            }

            // The water is stretched over its area by its scale; the walls undo it and keep their vertices in plain metres
            Vector3 scale = surface.lossyScale;
            walls.localPosition = Vector3.zero;
            walls.localRotation = Quaternion.identity;
            walls.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            walls.gameObject.layer = LayerMask.NameToLayer(WallsLayer);

            return walls;
        }

        // Marching squares: every grid cell the wall line crosses gives a piece of the ribbon. Returns the length of the wall
        private static float Ribbon(float[,] ground, Vector2 origin, float line, float bottom, float top, Transform walls,
            List<Vector3> vertices, List<int> triangles)
        {
            float length = 0f;
            int cellsX = ground.GetLength(0) - 1;
            int cellsZ = ground.GetLength(1) - 1;

            for (int x = 0; x < cellsX; x++)
            for (int z = 0; z < cellsZ; z++)
            {
                var cell = new Cell
                {
                    // The grid is framed by one ring of nodes, hence the step back
                    Origin = origin + new Vector2(x - 1, z - 1) * Step,
                    NearLeft = ground[x, z],
                    NearRight = ground[x + 1, z],
                    FarRight = ground[x + 1, z + 1],
                    FarLeft = ground[x, z + 1],
                    Line = line
                };

                int deep = (cell.Deep(Cell.NearLeftCorner) ? 1 : 0) | (cell.Deep(Cell.NearRightCorner) ? 2 : 0) |
                           (cell.Deep(Cell.FarRightCorner) ? 4 : 0) | (cell.Deep(Cell.FarLeftCorner) ? 8 : 0);
                if (deep == 0 || deep == 15) continue;

                bool saddle = deep == 5 || deep == 10;
                if (!saddle)
                {
                    int from = -1;
                    int to = -1;
                    for (int edge = 0; edge < 4; edge++)
                    {
                        if (!cell.Crosses(edge)) continue;

                        if (from < 0) from = edge;
                        else to = edge;
                    }

                    int witness = 0;
                    while (!cell.Deep(witness)) witness++;

                    length += Piece(cell, from, to, witness, bottom, top, walls, vertices, triangles);
                    continue;
                }

                // Two deep corners across each other: the middle of the cell tells whether the deep water joins them or the shallow parts them
                bool joined = (cell.NearLeft + cell.NearRight + cell.FarRight + cell.FarLeft) / 4f < line;
                bool nearLeftCut = deep == 5 ? !joined : joined;

                if (nearLeftCut)
                {
                    length += Piece(cell, Cell.Near, Cell.Left, Cell.NearLeftCorner, bottom, top, walls, vertices, triangles);
                    length += Piece(cell, Cell.Right, Cell.Far, Cell.FarRightCorner, bottom, top, walls, vertices, triangles);
                }
                else
                {
                    length += Piece(cell, Cell.Near, Cell.Right, Cell.NearRightCorner, bottom, top, walls, vertices, triangles);
                    length += Piece(cell, Cell.Far, Cell.Left, Cell.FarLeftCorner, bottom, top, walls, vertices, triangles);
                }
            }

            return length;
        }

        // A piece runs between two edges of a cell and has the witness corner on one side of it, alone or with other corners like it
        private static float Piece(Cell cell, int fromEdge, int toEdge, int witness, float bottom, float top, Transform walls,
            List<Vector3> vertices, List<int> triangles)
        {
            Vector2 from = cell.Crossing(fromEdge);
            Vector2 to = cell.Crossing(toEdge);

            Vector2 along = to - from;
            float length = along.magnitude;
            if (length < Shortest) return 0f;

            // The deep water is kept on the left of the piece, then the face of the quad looks at the shore
            Vector2 aside = cell.Position(witness) - from;
            bool witnessOnLeft = along.x * aside.y - along.y * aside.x > 0f;
            if (witnessOnLeft != cell.Deep(witness)) (from, to) = (to, from);

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

        // One square of the grid seen from above: x runs to the right, z runs away
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

            public bool Deep(int corner)
            {
                return Ground(corner) < Line;
            }

            public Vector2 Position(int corner)
            {
                return corner switch
                {
                    NearLeftCorner => Origin,
                    NearRightCorner => Origin + new Vector2(Step, 0f),
                    FarRightCorner => Origin + new Vector2(Step, Step),
                    _ => Origin + new Vector2(0f, Step)
                };
            }

            public bool Crosses(int edge)
            {
                (int from, int to) = Ends(edge);
                return Deep(from) != Deep(to);
            }

            // Where the wall line crosses the edge; both cells that share the edge get the very same point
            public Vector2 Crossing(int edge)
            {
                (int from, int to) = Ends(edge);
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

            // Every edge runs the same way in both cells that share it: to the right or away
            private static (int from, int to) Ends(int edge)
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
