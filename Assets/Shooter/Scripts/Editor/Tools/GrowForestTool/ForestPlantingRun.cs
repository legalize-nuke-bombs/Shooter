using System;
using System.Collections.Generic;
using Shooter.Game;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Shooter.Editor
{
    public class ForestPlantingRun
    {
        public const float TreelineFade = 60f;
        private const int Seed = 1;
        private const int Tries = 6;
        private const int DensitySide = 512;
        private const int ShoreProbes = 8;
        private const int MostHits = 64;
        private const int CellsPerProgressStep = 8192;
        private const float NoiseOrigin = 4096f;
        private const float NoiseStride = 37.3f;
        private const float NoiseLow = 0.3f;
        private const float NoiseHigh = 0.7f;
        private const float TrunkRadius = 0.4f;
        private const float MostShade = 0.4f;
        private const float RayHeadroom = 10f;

        private sealed class Tile
        {
            public Terrain Terrain;
            public TerrainData Data;
            public Rect Area;
            public float BaseHeight;
            public float[,] Density;
            public TreePrototype[] Palette;
            public int[] PrototypeOfSpecies;
            public List<TreeInstance> Trees;

            public Vector2 UvOf(Vector2 position)
            {
                return new Vector2((position.x - Area.x) / Area.width, (position.y - Area.y) / Area.height);
            }
        }

        private struct Spot
        {
            public Vector2 Position;
            public float Reach;
        }

        private sealed class SpacingGrid
        {
            private readonly Vector2 origin;
            private readonly float cell;
            private readonly int columns;
            private readonly int rows;
            private readonly int[] heads;
            private readonly List<int> links = new();
            private readonly List<Spot> spots = new();
            private float widest;

            public SpacingGrid(Rect area, float spacing)
            {
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
                int[] cells = new int[(right - left + 1) * (top - bottom + 1)];
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

            public Vector2 Center(int index)
            {
                return origin + cell * new Vector2(index % columns + 0.5f, index / columns + 0.5f);
            }

            public Vector2 Within(int index, Random random)
            {
                return origin + cell * new Vector2(index % columns + (float)random.NextDouble(), index / columns + (float)random.NextDouble());
            }

            public void Add(Vector2 position, float reach)
            {
                int index = Row(position.y) * columns + Column(position.x);
                links.Add(heads[index]);
                heads[index] = spots.Count;
                spots.Add(new Spot { Position = position, Reach = reach });
                widest = Mathf.Max(widest, reach);
            }

            public bool Free(Vector2 position)
            {
                int span = Mathf.CeilToInt(widest / cell);
                int column = Column(position.x);
                int row = Row(position.y);
                for (int y = Mathf.Max(row - span, 0); y <= Mathf.Min(row + span, rows - 1); y++)
                {
                    for (int x = Mathf.Max(column - span, 0); x <= Mathf.Min(column + span, columns - 1); x++)
                    {
                        for (int i = heads[y * columns + x]; i >= 0; i = links[i])
                        {
                            Spot spot = spots[i];
                            if ((spot.Position - position).sqrMagnitude < spot.Reach * spot.Reach) return false;
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

        private readonly ForestPlantingSettings settings;
        private readonly List<ForestPlantingSettings.Species> species;
        private readonly float[] weights;
        private readonly List<Terrain> landscape;
        private readonly Rect[] landscapeAreas;
        private readonly Water[] waters;
        private readonly float ceiling;
        private readonly Random random = new(Seed);
        private readonly RaycastHit[] hits = new RaycastHit[MostHits];
        private List<Tile> tiles;
        private SpacingGrid grid;
        private int planted;
        private int rejectedAsSteep;
        private int rejectedNearWater;
        private int rejectedAtObjects;

        public ForestPlantingRun(ForestPlantingSettings settings, List<Terrain> landscape)
        {
            this.settings = settings;
            this.landscape = landscape;
            species = settings.species.FindAll(entry => entry.tree != null);
            weights = new float[species.Count];
            landscapeAreas = landscape.ConvertAll(AreaOf).ToArray();
            waters = Object.FindObjectsByType<Water>(FindObjectsInactive.Exclude);
            ceiling = float.MinValue;
            foreach (Terrain terrain in landscape)
            {
                ceiling = Mathf.Max(ceiling, terrain.transform.position.y + terrain.terrainData.size.y + RayHeadroom);
            }
        }

        public string Plant(List<Terrain> targets)
        {
            DateTime started = DateTime.Now;
            Physics.SyncTransforms();

            tiles = targets.ConvertAll(TileOf);
            int kept = 0;
            foreach (Tile tile in tiles)
            {
                kept += tile.Trees.Count;
            }

            grid = new SpacingGrid(BoundingArea(landscapeAreas), settings.spacing);
            RegisterStandingTrees(targets);
            int[] order = grid.Cells(BoundingArea(targets.ConvertAll(AreaOf)));
            Shuffle(order);

            try
            {
                for (int i = 0; i < order.Length; i++)
                {
                    if (i % CellsPerProgressStep == 0 && EditorUtility.DisplayCancelableProgressBar("Grow Forest", $"{planted} trees", (float)i / order.Length))
                    {
                        return "Growing cancelled, nothing changed";
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

            return $"Grew {planted} trees on {tiles.Count} terrains in {(DateTime.Now - started).TotalSeconds:F1} s, kept {kept} rocks and bushes. " +
                   $"Spots turned down: {rejectedAsSteep} too steep, {rejectedNearWater} by water, {rejectedAtObjects} at objects";
        }

        private Tile TileOf(Terrain terrain)
        {
            TerrainData data = terrain.terrainData;
            TreePrototype[] prototypes = data.treePrototypes;
            var palette = new List<TreePrototype>(prototypes);
            Tile tile = new()
            {
                Terrain = terrain,
                Data = data,
                Area = AreaOf(terrain),
                BaseHeight = terrain.transform.position.y,
                Density = DensityMap(data),
                PrototypeOfSpecies = new int[species.Count],
                Trees = new List<TreeInstance>()
            };

            for (int i = 0; i < species.Count; i++)
            {
                GameObject tree = species[i].tree;
                int index = palette.FindIndex(prototype => prototype.prefab == tree);
                if (index < 0)
                {
                    palette.Add(new TreePrototype { prefab = tree });
                    index = palette.Count - 1;
                }

                tile.PrototypeOfSpecies[i] = index;
            }

            tile.Palette = palette.ToArray();
            foreach (TreeInstance instance in data.treeInstances)
            {
                if (!IsForestSpecies(prototypes[instance.prototypeIndex].prefab))
                {
                    tile.Trees.Add(instance);
                }
            }

            return tile;
        }

        private float[,] DensityMap(TerrainData data)
        {
            TerrainLayer[] palette = data.terrainLayers;
            float[] layerDensity = new float[palette.Length];
            for (int layer = 0; layer < palette.Length; layer++)
            {
                layerDensity[layer] = 1f;
                foreach (ForestPlantingSettings.LayerDensity entry in settings.layerDensities)
                {
                    if (entry.layer == palette[layer])
                    {
                        layerDensity[layer] = entry.density;
                    }
                }
            }

            int side = data.alphamapResolution;
            int step = Mathf.Max(1, side / DensitySide);
            int reduced = side / step;
            float[,] density = new float[reduced, reduced];
            for (int y = 0; y < reduced; y++)
            {
                float[,,] row = data.GetAlphamaps(0, y * step, side, 1);
                for (int x = 0; x < reduced; x++)
                {
                    float total = 0f;
                    float weighted = 0f;
                    for (int layer = 0; layer < palette.Length; layer++)
                    {
                        float alpha = row[0, x * step, layer];
                        total += alpha;
                        weighted += alpha * layerDensity[layer];
                    }

                    density[y, x] = total > 0f ? weighted / total : 1f;
                }
            }

            return density;
        }

        private void RegisterStandingTrees(List<Terrain> targets)
        {
            var footprints = new Dictionary<GameObject, float>();
            foreach (Terrain terrain in landscape)
            {
                TerrainData data = terrain.terrainData;
                TreePrototype[] prototypes = data.treePrototypes;
                Rect area = AreaOf(terrain);
                bool regrown = targets.Contains(terrain);
                foreach (TreeInstance instance in data.treeInstances)
                {
                    GameObject prefab = prototypes[instance.prototypeIndex].prefab;
                    Vector2 position = new(area.x + instance.position.x * area.width, area.y + instance.position.z * area.height);
                    if (IsForestSpecies(prefab))
                    {
                        if (!regrown)
                        {
                            grid.Add(position, settings.spacing);
                        }

                        continue;
                    }

                    if (!footprints.TryGetValue(prefab, out float footprint))
                    {
                        footprint = Footprint(prefab);
                        footprints.Add(prefab, footprint);
                    }

                    grid.Add(position, footprint * instance.widthScale + settings.clearanceAroundObjects);
                }
            }
        }

        private bool IsForestSpecies(GameObject prefab)
        {
            return species.Exists(entry => entry.tree == prefab);
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
                    Vector3 sign = new((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
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
                int j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }

        private void Sow(int cell)
        {
            Vector2 center = grid.Center(cell);
            Tile centerTile = TileAt(center);
            if (centerTile == null) return;
            if (random.NextDouble() >= Chance(centerTile, center)) return;

            for (int attempt = 0; attempt < Tries; attempt++)
            {
                Vector2 position = grid.Within(cell, random);
                Tile tile = TileAt(position);
                if (tile == null) continue;
                if (!grid.Free(position)) continue;

                Vector2 uv = tile.UvOf(position);
                float slope = tile.Data.GetSteepness(uv.x, uv.y);
                if (slope > settings.steepestSlope)
                {
                    rejectedAsSteep++;
                    continue;
                }

                float height = tile.Data.GetInterpolatedHeight(uv.x, uv.y);
                Vector3 foot = new(position.x, tile.BaseHeight + height, position.y);
                if (NearWater(foot))
                {
                    rejectedNearWater++;
                    continue;
                }

                if (Blocked(foot))
                {
                    rejectedAtObjects++;
                    continue;
                }

                Plant(tile, position, uv, height - TrunkRadius * Mathf.Tan(slope * Mathf.Deg2Rad));
                return;
            }
        }

        private Tile TileAt(Vector2 position)
        {
            foreach (Tile tile in tiles)
            {
                if (tile.Area.Contains(position)) return tile;
            }

            return null;
        }

        private float Chance(Tile tile, Vector2 position)
        {
            Vector2 uv = tile.UvOf(position);
            int side = tile.Density.GetLength(0);
            float ground = tile.Density[Mathf.Min((int)(uv.y * side), side - 1), Mathf.Min((int)(uv.x * side), side - 1)];
            float height = tile.Data.GetInterpolatedHeight(uv.x, uv.y);
            float altitude = Mathf.Clamp01((settings.treelineHeight - tile.BaseHeight - height) / TreelineFade);
            return ground * altitude * Mathf.Lerp(1f, 2f * Noise(position, 0), settings.patchiness);
        }

        private float Noise(Vector2 position, int layer)
        {
            float stride = layer * NoiseStride;
            return Mathf.InverseLerp(NoiseLow, NoiseHigh, Mathf.PerlinNoise(NoiseOrigin + stride + position.x / settings.groveSize, NoiseOrigin - stride + position.y / settings.groveSize));
        }

        private bool NearWater(Vector3 foot)
        {
            if (Submerged(foot)) return true;

            for (int i = 0; i < ShoreProbes; i++)
            {
                float angle = 2f * Mathf.PI * i / ShoreProbes;
                Vector3 probe = new(foot.x + settings.distanceFromWater * Mathf.Cos(angle), 0f, foot.z + settings.distanceFromWater * Mathf.Sin(angle));
                int index = TerrainIndexAt(probe);
                if (index < 0) continue;

                Terrain terrain = landscape[index];
                probe.y = terrain.transform.position.y + terrain.SampleHeight(probe);
                if (Submerged(probe)) return true;
            }

            return false;
        }

        private int TerrainIndexAt(Vector3 point)
        {
            for (int i = 0; i < landscapeAreas.Length; i++)
            {
                if (landscapeAreas[i].Contains(new Vector2(point.x, point.z))) return i;
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
            Vector3 origin = new(foot.x, ceiling, foot.z);
            int count = Physics.SphereCastNonAlloc(origin, settings.clearanceAroundObjects, Vector3.down, hits, ceiling - foot.y, settings.obstacles, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider is not TerrainCollider) return true;
            }

            return false;
        }

        private void Plant(Tile tile, Vector2 position, Vector2 uv, float height)
        {
            int picked = PickSpecies(position);
            Vector2 scaleRange = species[picked].scaleRange;
            float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)random.NextDouble());
            byte gray = (byte)(255f * (1f - MostShade * (float)random.NextDouble()));
            tile.Trees.Add(new TreeInstance
            {
                position = new Vector3(uv.x, height / tile.Data.size.y, uv.y),
                widthScale = scale,
                heightScale = scale,
                rotation = (float)(random.NextDouble() * Math.PI * 2.0),
                color = new Color32(gray, gray, gray, 255),
                lightmapColor = new Color32(255, 255, 255, 255),
                prototypeIndex = tile.PrototypeOfSpecies[picked]
            });
            grid.Add(position, settings.spacing);
            planted++;
        }

        private int PickSpecies(Vector2 position)
        {
            float total = 0f;
            for (int i = 0; i < species.Count; i++)
            {
                weights[i] = species[i].share * Mathf.Lerp(1f, 2f * Noise(position, i + 1), settings.patchiness);
                total += weights[i];
            }

            double roll = random.NextDouble() * total;
            int picked = 0;
            for (int i = 0; i < species.Count; i++)
            {
                if (weights[i] <= 0f) continue;

                picked = i;
                roll -= weights[i];
                if (roll < 0) break;
            }

            return picked;
        }

        private static Rect AreaOf(Terrain terrain)
        {
            Vector3 corner = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            return new Rect(corner.x, corner.z, size.x, size.z);
        }

        private static Rect BoundingArea(IEnumerable<Rect> areas)
        {
            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);
            foreach (Rect area in areas)
            {
                min = Vector2.Min(min, area.min);
                max = Vector2.Max(max, area.max);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
