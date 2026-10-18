using System;
using System.Collections.Generic;
using Shooter.Game;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Editor
{
    public class ForestGrower : EditorWindow
    {
        private const string LayerFolder = "Assets/Shooter/Data/Environment/Terrain/Layers/";
        private const string TreeFolder = "Assets/Shooter/Data/Environment/Props/Nature/Trees/";
        private const int Seed = 1;
        private const int Tries = 6;
        private const int DensitySide = 512;
        private const int ShoreProbes = 8;
        private const int MostHits = 64;
        private const int CellsPerStep = 8192;
        private const float NoiseOrigin = 4096f;
        private const float NoiseStride = 37.3f;
        private const float NoiseLow = 0.3f;
        private const float NoiseHigh = 0.7f;
        private const float TreelineFade = 60f;
        private const float TrunkRadius = 0.4f;
        private const float Shade = 0.4f;
        private const float RayHeadroom = 10f;
        private static readonly string[] Unrooted = { "Character", "Hitbox", "FirstPerson", "ReflectionOnly", "Item" };
        private static readonly Journal Log = Logs.Here();

        [Serializable]
        private class Species
        {
            public GameObject tree;
            public float share = 1f;
            public Vector2 size = new(0.8f, 1.2f);
        }

        [Serializable]
        private class Ground
        {
            public TerrainLayer layer;
            [Range(0f, 1f)] public float density = 1f;
        }

        private sealed class Tile
        {
            public Terrain Terrain;
            public TerrainData Data;
            public Rect Area;
            public float Base;
            public float[,] Density;
            public TreePrototype[] Palette;
            public int[] Prototypes;
            public List<TreeInstance> Trees;
        }

        private sealed class Trunks
        {
            private readonly Vector2 origin;
            private readonly float cell;
            private readonly float spacing;
            private readonly int columns;
            private readonly int rows;
            private readonly int[] heads;
            private readonly List<int> links = new();
            private readonly List<Vector3> spots = new();
            private float widest;

            public Trunks(Rect area, float spacing)
            {
                this.spacing = spacing;
                cell = spacing / Mathf.Sqrt(2f);
                origin = area.min;
                columns = Mathf.CeilToInt(area.width / cell);
                rows = Mathf.CeilToInt(area.height / cell);
                heads = new int[columns * rows];
                Array.Fill(heads, -1);
                widest = spacing;
            }

            public int[] Cells(Rect area)
            {
                int left = Column(area.xMin);
                int right = Column(area.xMax);
                int bottom = Row(area.yMin);
                int top = Row(area.yMax);
                var cells = new int[(right - left + 1) * (top - bottom + 1)];
                int next = 0;
                for (int row = bottom; row <= top; row++)
                {
                    for (int column = left; column <= right; column++)
                    {
                        cells[next++] = row * columns + column;
                    }
                }

                return cells;
            }

            public Vector2 Within(int index, System.Random dice)
            {
                return origin + cell * new Vector2(index % columns + (float)dice.NextDouble(), index / columns + (float)dice.NextDouble());
            }

            public void Add(Vector2 point, float reach)
            {
                int index = Row(point.y) * columns + Column(point.x);
                links.Add(heads[index]);
                heads[index] = spots.Count;
                spots.Add(new Vector3(point.x, point.y, reach));
                widest = Mathf.Max(widest, reach);
            }

            public bool Free(Vector2 point)
            {
                int span = Mathf.CeilToInt(widest / cell);
                int column = Column(point.x);
                int row = Row(point.y);
                for (int y = Mathf.Max(row - span, 0); y <= Mathf.Min(row + span, rows - 1); y++)
                {
                    for (int x = Mathf.Max(column - span, 0); x <= Mathf.Min(column + span, columns - 1); x++)
                    {
                        for (int i = heads[y * columns + x]; i >= 0; i = links[i])
                        {
                            Vector3 spot = spots[i];
                            float limit = spot.z > 0f ? spot.z : spacing;
                            if ((new Vector2(spot.x, spot.y) - point).sqrMagnitude < limit * limit) return false;
                        }
                    }
                }

                return true;
            }

            private int Column(float x)
            {
                return Mathf.Clamp(Mathf.FloorToInt((x - origin.x) / cell), 0, columns - 1);
            }

            private int Row(float z)
            {
                return Mathf.Clamp(Mathf.FloorToInt((z - origin.y) / cell), 0, rows - 1);
            }
        }

        [SerializeField] private List<Species> species = new();
        [SerializeField] private List<Ground> grounds = new();
        [SerializeField] private float spacing = 8f;
        [SerializeField] private float clump = 60f;
        [SerializeField, Range(0f, 1f)] private float patchiness = 0.5f;
        [SerializeField] private float steepest = 40f;
        [SerializeField] private float treeline = 350f;
        [SerializeField] private float shore = 4f;
        [SerializeField] private float clearance = 2f;

        private readonly RaycastHit[] hits = new RaycastHit[MostHits];
        private string result;
        private SerializedObject serialized;
        private List<Species> chosen;
        private float[] weights;
        private List<Tile> tiles;
        private List<Terrain> landscape;
        private Rect[] outlines;
        private Water[] waters;
        private Trunks trunks;
        private System.Random dice;
        private int solid;
        private float ceiling;
        private int grown;
        private int steep;
        private int wet;
        private int blocked;

        private void OnEnable()
        {
            if (species.Count == 0) Defaults();

            serialized = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(species)), new GUIContent("Species"), true);
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(grounds)), new GUIContent("Density on ground layers"), true);
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(spacing)), new GUIContent("Spacing between trunks, m"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(clump)), new GUIContent("Grove size, m"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(patchiness)), new GUIContent("Patchiness"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(steepest)), new GUIContent("Steepest slope, degrees"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(treeline)), new GUIContent("Treeline height, m"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(shore)), new GUIContent("Distance from water, m"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(clearance)), new GUIContent("Clearance around objects, m"));
            serialized.ApplyModifiedProperties();

            EditorGUILayout.LabelField("Forest is full on layers missing from the list and thins out over " +
                                       $"{TreelineFade} m below the treeline. Share is a species' part of the forest, size is the range of its scale. " +
                                       "Growing replaces the trees of these species on the chosen terrains, rocks and bushes stay and trees keep clear " +
                                       "of them and of anything with a collider", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            List<Terrain> targets = Landscape.Targets();
            EditorGUILayout.LabelField(Landscape.Selected() ? $"{targets.Count} selected terrains" : $"{targets.Count} terrains of the landscape");

            if (GUILayout.Button("Grow forest")) Grow();
            if (GUILayout.Button("Defaults"))
            {
                Undo.RecordObject(this, "Forest grower defaults");
                Defaults();
                serialized.Update();
            }

            if (!string.IsNullOrEmpty(result)) EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
        }

        [MenuItem("Tools/Grow Forest")]
        private static void Open()
        {
            GetWindow<ForestGrower>("Grow Forest");
        }

        private void Defaults()
        {
            spacing = 8f;
            clump = 60f;
            patchiness = 0.5f;
            steepest = 40f;
            treeline = 350f;
            shore = 4f;
            clearance = 2f;
            species = new List<Species>
            {
                Of("Pines/Pine_B/Pine_B", 0.3f, 0.7f, 1.2f),
                Of("Cypress/Cypress", 0.25f, 1f, 1.8f),
                Of("Conifer/Conifer", 0.15f, 0.7f, 1.2f),
                Of("Pines/Pine_A/Pine_A", 0.1f, 0.7f, 1.2f),
                Of("Pines/Pine_C/Pine_C", 0.1f, 0.8f, 1.4f),
                Of("Pines/Pine_D/Pine_D", 0.1f, 0.8f, 1.4f)
            };
            grounds = new List<Ground>
            {
                On("Grass_Moss_A", 1f), On("Grass_A", 1f), On("Grass_B", 1f), On("Grass_Soil_A", 1f),
                On("Pebbles_B", 0.5f), On("Heather_A", 0.3f), On("Cliff_Mossy_E", 0.2f), On("Tidal_Pools_B", 0f)
            };
        }

        private static Species Of(string tree, float share, float smallest, float largest)
        {
            return new Species
            {
                tree = AssetDatabase.LoadAssetAtPath<GameObject>(TreeFolder + tree + ".prefab"),
                share = share,
                size = new Vector2(smallest, largest)
            };
        }

        private static Ground On(string layer, float density)
        {
            return new Ground { layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerFolder + layer + ".terrainlayer"), density = density };
        }

        private void Grow()
        {
            if (EditorApplication.isPlaying)
            {
                Log.Error("Forest grows in edit mode only, stop the play mode first");
                return;
            }

            List<Terrain> targets = Landscape.Targets();
            if (targets.Count == 0)
            {
                Log.Error("Scene has no terrain to grow forest on");
                return;
            }

            chosen = species.FindAll(kind => kind.tree != null);
            if (chosen.Count == 0 || chosen.TrueForAll(kind => kind.share <= 0f))
            {
                Log.Error("Forest grower has no species with a tree and a share, nothing grown");
                return;
            }

            var started = DateTime.Now;
            weights = new float[chosen.Count];
            landscape = Landscape.All();
            outlines = landscape.ConvertAll(Outline).ToArray();
            waters = Object.FindObjectsByType<Water>(FindObjectsInactive.Exclude);
            solid = Physics.DefaultRaycastLayers & ~LayerMask.GetMask(Unrooted);
            dice = new System.Random(Seed);
            grown = 0;
            steep = 0;
            wet = 0;
            blocked = 0;
            ceiling = float.MinValue;
            foreach (Terrain terrain in landscape)
            {
                ceiling = Mathf.Max(ceiling, terrain.transform.position.y + terrain.terrainData.size.y + RayHeadroom);
            }

            Physics.SyncTransforms();

            tiles = targets.ConvertAll(Prepare);
            int kept = 0;
            foreach (Tile tile in tiles)
            {
                kept += tile.Trees.Count;
            }

            trunks = new Trunks(Enclose(outlines), spacing);
            Survey(targets);
            int[] order = trunks.Cells(Enclose(targets.ConvertAll(Outline)));
            Shuffle(order);

            try
            {
                for (int i = 0; i < order.Length; i++)
                {
                    if (i % CellsPerStep == 0 && EditorUtility.DisplayCancelableProgressBar("Grow Forest", $"{grown} trees", (float)i / order.Length))
                    {
                        result = "Growing cancelled, nothing changed";
                        return;
                    }

                    Sow(order[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (Tile tile in tiles)
            {
                tile.Data.treePrototypes = tile.Palette;
                tile.Data.SetTreeInstances(tile.Trees.ToArray(), false);
                tile.Terrain.Flush();
                EditorUtility.SetDirty(tile.Data);
            }

            result = $"Grew {grown} trees on {tiles.Count} terrains in {(DateTime.Now - started).TotalSeconds:F1} s, kept {kept} rocks and bushes. " +
                     $"Spots turned down: {steep} too steep, {wet} by water, {blocked} at objects";
            Log.Info(result);
        }

        private Tile Prepare(Terrain terrain)
        {
            TerrainData data = terrain.terrainData;
            TreePrototype[] prototypes = data.treePrototypes;
            var palette = new List<TreePrototype>(prototypes);
            var tile = new Tile
            {
                Terrain = terrain,
                Data = data,
                Area = Outline(terrain),
                Base = terrain.transform.position.y,
                Density = Density(data),
                Prototypes = new int[chosen.Count],
                Trees = new List<TreeInstance>()
            };

            for (int k = 0; k < chosen.Count; k++)
            {
                GameObject tree = chosen[k].tree;
                int index = palette.FindIndex(prototype => prototype.prefab == tree);
                if (index < 0)
                {
                    palette.Add(new TreePrototype { prefab = tree });
                    index = palette.Count - 1;
                }

                tile.Prototypes[k] = index;
            }

            tile.Palette = palette.ToArray();
            foreach (TreeInstance instance in data.treeInstances)
            {
                if (!Ours(prototypes[instance.prototypeIndex].prefab))
                {
                    tile.Trees.Add(instance);
                }
            }

            return tile;
        }

        private float[,] Density(TerrainData data)
        {
            TerrainLayer[] palette = data.terrainLayers;
            var rates = new float[palette.Length];
            for (int l = 0; l < palette.Length; l++)
            {
                rates[l] = 1f;
                foreach (Ground ground in grounds)
                {
                    if (ground.layer == palette[l])
                    {
                        rates[l] = ground.density;
                    }
                }
            }

            int side = data.alphamapResolution;
            int step = Mathf.Max(1, side / DensitySide);
            int reduced = side / step;
            var density = new float[reduced, reduced];
            for (int y = 0; y < reduced; y++)
            {
                float[,,] row = data.GetAlphamaps(0, y * step, side, 1);
                for (int x = 0; x < reduced; x++)
                {
                    float total = 0f;
                    float weighted = 0f;
                    for (int l = 0; l < palette.Length; l++)
                    {
                        float alpha = row[0, x * step, l];
                        total += alpha;
                        weighted += alpha * rates[l];
                    }

                    density[y, x] = total > 0f ? weighted / total : 1f;
                }
            }

            return density;
        }

        private void Survey(List<Terrain> targets)
        {
            var footprints = new Dictionary<GameObject, float>();
            foreach (Terrain terrain in landscape)
            {
                TerrainData data = terrain.terrainData;
                TreePrototype[] prototypes = data.treePrototypes;
                Rect area = Outline(terrain);
                bool regrown = targets.Contains(terrain);
                foreach (TreeInstance instance in data.treeInstances)
                {
                    GameObject prefab = prototypes[instance.prototypeIndex].prefab;
                    var point = new Vector2(area.x + instance.position.x * area.width, area.y + instance.position.z * area.height);
                    if (Ours(prefab))
                    {
                        if (!regrown)
                        {
                            trunks.Add(point, 0f);
                        }

                        continue;
                    }

                    if (!footprints.TryGetValue(prefab, out float footprint))
                    {
                        footprint = Footprint(prefab);
                        footprints.Add(prefab, footprint);
                    }

                    trunks.Add(point, footprint * instance.widthScale + clearance);
                }
            }
        }

        private bool Ours(GameObject prefab)
        {
            foreach (Species kind in chosen)
            {
                if (kind.tree == prefab) return true;
            }

            return false;
        }

        private static float Footprint(GameObject prefab)
        {
            Matrix4x4 root = prefab.transform.worldToLocalMatrix;
            float widest = 0f;
            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;

                Bounds bounds = filter.sharedMesh.bounds;
                Matrix4x4 local = root * filter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    var sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    Vector3 point = local.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, sign));
                    widest = Mathf.Max(widest, new Vector2(point.x, point.z).magnitude);
                }
            }

            return widest;
        }

        private void Shuffle(int[] order)
        {
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = dice.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }

        private void Sow(int cell)
        {
            bool decided = false;
            for (int attempt = 0; attempt < Tries; attempt++)
            {
                Vector2 point = trunks.Within(cell, dice);
                Tile tile = TileAt(point);
                if (tile == null) continue;

                float u = (point.x - tile.Area.x) / tile.Area.width;
                float v = (point.y - tile.Area.y) / tile.Area.height;
                float height = tile.Data.GetInterpolatedHeight(u, v);
                if (!decided)
                {
                    decided = true;
                    if (dice.NextDouble() >= Chance(tile, u, v, point, height)) return;
                }

                if (!trunks.Free(point)) continue;

                float slope = tile.Data.GetSteepness(u, v);
                if (slope > steepest)
                {
                    steep++;
                    continue;
                }

                var foot = new Vector3(point.x, tile.Base + height, point.y);
                if (Wet(foot))
                {
                    wet++;
                    continue;
                }

                if (Blocked(foot))
                {
                    blocked++;
                    continue;
                }

                Plant(tile, point, u, v, height - TrunkRadius * Mathf.Tan(slope * Mathf.Deg2Rad));
                return;
            }
        }

        private Tile TileAt(Vector2 point)
        {
            foreach (Tile tile in tiles)
            {
                if (tile.Area.Contains(point)) return tile;
            }

            return null;
        }

        private float Chance(Tile tile, float u, float v, Vector2 point, float height)
        {
            int side = tile.Density.GetLength(0);
            float ground = tile.Density[Mathf.Min((int)(v * side), side - 1), Mathf.Min((int)(u * side), side - 1)];
            float altitude = Mathf.Clamp01((treeline - tile.Base - height) / TreelineFade);
            return ground * altitude * Mathf.Lerp(1f, 2f * Noise(point, 0), patchiness);
        }

        private float Noise(Vector2 point, int layer)
        {
            float stride = layer * NoiseStride;
            return Mathf.InverseLerp(NoiseLow, NoiseHigh, Mathf.PerlinNoise(NoiseOrigin + stride + point.x / clump, NoiseOrigin - stride + point.y / clump));
        }

        private bool Wet(Vector3 foot)
        {
            if (Submerged(foot)) return true;

            for (int i = 0; i < ShoreProbes; i++)
            {
                float angle = 2f * Mathf.PI * i / ShoreProbes;
                var probe = new Vector3(foot.x + shore * Mathf.Cos(angle), 0f, foot.z + shore * Mathf.Sin(angle));
                int under = Under(probe);
                if (under < 0) continue;

                Terrain terrain = landscape[under];
                probe.y = terrain.transform.position.y + terrain.SampleHeight(probe);
                if (Submerged(probe)) return true;
            }

            return false;
        }

        private int Under(Vector3 point)
        {
            for (int i = 0; i < outlines.Length; i++)
            {
                if (outlines[i].Contains(new Vector2(point.x, point.z))) return i;
            }

            return -1;
        }

        private bool Submerged(Vector3 point)
        {
            foreach (Water water in waters)
            {
                if (water.DepthAt(point) > 0f) return true;
            }

            return false;
        }

        private bool Blocked(Vector3 foot)
        {
            var origin = new Vector3(foot.x, ceiling, foot.z);
            int count = Physics.SphereCastNonAlloc(origin, clearance, Vector3.down, hits, ceiling - foot.y, solid, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider is not TerrainCollider) return true;
            }

            return false;
        }

        private void Plant(Tile tile, Vector2 point, float u, float v, float height)
        {
            int kind = Pick(point);
            Vector2 size = chosen[kind].size;
            float scale = Mathf.Lerp(size.x, size.y, (float)dice.NextDouble());
            var gray = (byte)(255f * (1f - Shade * (float)dice.NextDouble()));
            tile.Trees.Add(new TreeInstance
            {
                position = new Vector3(u, height / tile.Data.size.y, v),
                widthScale = scale,
                heightScale = scale,
                rotation = (float)(dice.NextDouble() * Math.PI * 2.0),
                color = new Color32(gray, gray, gray, 255),
                lightmapColor = new Color32(255, 255, 255, 255),
                prototypeIndex = tile.Prototypes[kind]
            });
            trunks.Add(point, 0f);
            grown++;
        }

        private int Pick(Vector2 point)
        {
            float total = 0f;
            for (int k = 0; k < chosen.Count; k++)
            {
                weights[k] = chosen[k].share * Mathf.Lerp(1f, 2f * Noise(point, k + 1), patchiness);
                total += weights[k];
            }

            double roll = dice.NextDouble() * total;
            int pick = 0;
            for (int k = 0; k < chosen.Count; k++)
            {
                if (weights[k] <= 0f) continue;

                pick = k;
                roll -= weights[k];
                if (roll < 0) break;
            }

            return pick;
        }

        private static Rect Outline(Terrain terrain)
        {
            Vector3 corner = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            return new Rect(corner.x, corner.z, size.x, size.z);
        }

        private static Rect Enclose(IEnumerable<Rect> rects)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (Rect rect in rects)
            {
                min = Vector2.Min(min, rect.min);
                max = Vector2.Max(max, rect.max);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
