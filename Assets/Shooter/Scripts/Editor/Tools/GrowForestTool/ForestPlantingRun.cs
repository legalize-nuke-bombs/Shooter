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
        private const int TriesPerCell = 6;
        private const int DensitySide = 512;
        private const int ShoreProbes = 8;
        private const int MostObstacleHits = 64;
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
            public TerrainData TerrainData;
            public Rect Area;
            public float BaseHeight;
            public float[,] DensityMap;
            public TreePrototype[] TreePrototypes;
            public int[] PrototypeIndexOfSpecies;
            public List<TreeInstance> Trees;

            public Vector2 GetUvAt(Vector2 position)
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
            private readonly Vector2 gridOrigin;
            private readonly float cellSize;
            private readonly int columnCount;
            private readonly int rowCount;
            private readonly int[] firstSpotInCell;
            private readonly List<int> nextSpotInSameCell = new();
            private readonly List<Spot> spots = new();
            private float widestReach;

            public SpacingGrid(Rect area, float spacing)
            {
                cellSize = spacing / Mathf.Sqrt(2f);
                gridOrigin = area.min;
                columnCount = Mathf.CeilToInt(area.width / cellSize);
                rowCount = Mathf.CeilToInt(area.height / cellSize);
                firstSpotInCell = new int[columnCount * rowCount];
                Array.Fill(firstSpotInCell, -1);
                widestReach = spacing;
            }

            public int[] GetCellIndicesCovering(Rect area)
            {
                int left = GetColumnAt(area.xMin);
                int right = GetColumnAt(area.xMax);
                int bottom = GetRowAt(area.yMin);
                int top = GetRowAt(area.yMax);
                int[] cellIndices = new int[(right - left + 1) * (top - bottom + 1)];
                int nextIndex = 0;
                for (int row = bottom; row <= top; row++)
                {
                    for (int column = left; column <= right; column++)
                    {
                        cellIndices[nextIndex++] = row * columnCount + column;
                    }
                }

                return cellIndices;
            }

            public Vector2 GetCellCenter(int cellIndex)
            {
                return gridOrigin + cellSize * new Vector2(cellIndex % columnCount + 0.5f, cellIndex / columnCount + 0.5f);
            }

            public Vector2 GetRandomPointInCell(int cellIndex, Random random)
            {
                return gridOrigin + cellSize * new Vector2(cellIndex % columnCount + (float)random.NextDouble(), cellIndex / columnCount + (float)random.NextDouble());
            }

            public void AddSpot(Vector2 position, float reach)
            {
                int cellIndex = GetRowAt(position.y) * columnCount + GetColumnAt(position.x);
                nextSpotInSameCell.Add(firstSpotInCell[cellIndex]);
                firstSpotInCell[cellIndex] = spots.Count;
                spots.Add(new Spot { Position = position, Reach = reach });
                widestReach = Mathf.Max(widestReach, reach);
            }

            public bool IsFreeAt(Vector2 position)
            {
                int cellSpan = Mathf.CeilToInt(widestReach / cellSize);
                int column = GetColumnAt(position.x);
                int row = GetRowAt(position.y);
                for (int y = Mathf.Max(row - cellSpan, 0); y <= Mathf.Min(row + cellSpan, rowCount - 1); y++)
                {
                    for (int x = Mathf.Max(column - cellSpan, 0); x <= Mathf.Min(column + cellSpan, columnCount - 1); x++)
                    {
                        for (int i = firstSpotInCell[y * columnCount + x]; i >= 0; i = nextSpotInSameCell[i])
                        {
                            Spot spot = spots[i];
                            if ((spot.Position - position).sqrMagnitude < spot.Reach * spot.Reach) return false;
                        }
                    }
                }

                return true;
            }

            private int GetColumnAt(float x)
            {
                return Mathf.Clamp(Mathf.FloorToInt((x - gridOrigin.x) / cellSize), 0, columnCount - 1);
            }

            private int GetRowAt(float z)
            {
                return Mathf.Clamp(Mathf.FloorToInt((z - gridOrigin.y) / cellSize), 0, rowCount - 1);
            }
        }

        private readonly ForestPlantingSettings settings;
        private readonly List<ForestPlantingSettings.Species> species;
        private readonly float[] speciesWeights;
        private readonly List<Terrain> allTerrains;
        private readonly Rect[] allTerrainAreas;
        private readonly Water[] waters;
        private readonly float ceilingHeight;
        private readonly Random random = new(Seed);
        private readonly RaycastHit[] obstacleHits = new RaycastHit[MostObstacleHits];
        private List<Tile> targetTiles;
        private SpacingGrid spacingGrid;
        private int plantedTrees;
        private int rejectedAsSteep;
        private int rejectedNearWater;
        private int rejectedAtObjects;

        public ForestPlantingRun(ForestPlantingSettings settings, List<Terrain> allTerrains)
        {
            this.settings = settings;
            this.allTerrains = allTerrains;
            species = settings.species.FindAll(entry => entry.tree != null);
            speciesWeights = new float[species.Count];
            allTerrainAreas = allTerrains.ConvertAll(GetAreaOf).ToArray();
            waters = Object.FindObjectsByType<Water>(FindObjectsInactive.Exclude);
            ceilingHeight = float.MinValue;
            foreach (Terrain terrain in allTerrains)
            {
                ceilingHeight = Mathf.Max(ceilingHeight, terrain.transform.position.y + terrain.terrainData.size.y + RayHeadroom);
            }
        }

        public string PlantForestOn(List<Terrain> targetTerrains)
        {
            DateTime startTime = DateTime.Now;
            Physics.SyncTransforms();

            targetTiles = targetTerrains.ConvertAll(CreateTile);
            int keptInstances = 0;
            foreach (Tile tile in targetTiles)
            {
                keptInstances += tile.Trees.Count;
            }

            spacingGrid = new SpacingGrid(GetBoundingArea(allTerrainAreas), settings.spacing);
            RegisterStandingTrees(targetTerrains);
            int[] cellOrder = spacingGrid.GetCellIndicesCovering(GetBoundingArea(targetTerrains.ConvertAll(GetAreaOf)));
            ShuffleInPlace(cellOrder);

            try
            {
                for (int i = 0; i < cellOrder.Length; i++)
                {
                    if (i % CellsPerProgressStep == 0 && EditorUtility.DisplayCancelableProgressBar("Grow Forest", $"{plantedTrees} trees", (float)i / cellOrder.Length))
                    {
                        return "Growing cancelled, nothing changed";
                    }

                    SowCell(cellOrder[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (Tile tile in targetTiles)
            {
                tile.TerrainData.treePrototypes = tile.TreePrototypes;
                tile.TerrainData.SetTreeInstances(tile.Trees.ToArray(), false);
                tile.Terrain.Flush();
                EditorUtility.SetDirty(tile.TerrainData);
            }

            return $"Grew {plantedTrees} trees on {targetTiles.Count} terrains in {(DateTime.Now - startTime).TotalSeconds:F1} s, kept {keptInstances} rocks and bushes. " +
                   $"Spots turned down: {rejectedAsSteep} too steep, {rejectedNearWater} by water, {rejectedAtObjects} at objects";
        }

        private Tile CreateTile(Terrain terrain)
        {
            TerrainData terrainData = terrain.terrainData;
            TreePrototype[] prototypes = terrainData.treePrototypes;
            var prototypesWithSpecies = new List<TreePrototype>(prototypes);
            Tile tile = new()
            {
                Terrain = terrain,
                TerrainData = terrainData,
                Area = GetAreaOf(terrain),
                BaseHeight = terrain.transform.position.y,
                DensityMap = CreateDensityMap(terrainData),
                PrototypeIndexOfSpecies = new int[species.Count],
                Trees = new List<TreeInstance>()
            };

            for (int i = 0; i < species.Count; i++)
            {
                GameObject treePrefab = species[i].tree;
                int prototypeIndex = prototypesWithSpecies.FindIndex(prototype => prototype.prefab == treePrefab);
                if (prototypeIndex < 0)
                {
                    prototypesWithSpecies.Add(new TreePrototype { prefab = treePrefab });
                    prototypeIndex = prototypesWithSpecies.Count - 1;
                }

                tile.PrototypeIndexOfSpecies[i] = prototypeIndex;
            }

            tile.TreePrototypes = prototypesWithSpecies.ToArray();
            foreach (TreeInstance instance in terrainData.treeInstances)
            {
                if (!IsForestSpecies(prototypes[instance.prototypeIndex].prefab))
                {
                    tile.Trees.Add(instance);
                }
            }

            return tile;
        }

        private float[,] CreateDensityMap(TerrainData terrainData)
        {
            TerrainLayer[] terrainLayers = terrainData.terrainLayers;
            float[] densityOfLayer = new float[terrainLayers.Length];
            for (int layer = 0; layer < terrainLayers.Length; layer++)
            {
                densityOfLayer[layer] = 1f;
                foreach (ForestPlantingSettings.LayerDensity entry in settings.layerDensities)
                {
                    if (entry.layer == terrainLayers[layer])
                    {
                        densityOfLayer[layer] = entry.density;
                    }
                }
            }

            int alphamapResolution = terrainData.alphamapResolution;
            int step = Mathf.Max(1, alphamapResolution / DensitySide);
            int reducedSide = alphamapResolution / step;
            float[,] densityMap = new float[reducedSide, reducedSide];
            for (int y = 0; y < reducedSide; y++)
            {
                float[,,] alphamapRow = terrainData.GetAlphamaps(0, y * step, alphamapResolution, 1);
                for (int x = 0; x < reducedSide; x++)
                {
                    float totalAlpha = 0f;
                    float weightedDensity = 0f;
                    for (int layer = 0; layer < terrainLayers.Length; layer++)
                    {
                        float alpha = alphamapRow[0, x * step, layer];
                        totalAlpha += alpha;
                        weightedDensity += alpha * densityOfLayer[layer];
                    }

                    densityMap[y, x] = totalAlpha > 0f ? weightedDensity / totalAlpha : 1f;
                }
            }

            return densityMap;
        }

        private void RegisterStandingTrees(List<Terrain> targetTerrains)
        {
            var footprintOfPrefab = new Dictionary<GameObject, float>();
            foreach (Terrain terrain in allTerrains)
            {
                TerrainData terrainData = terrain.terrainData;
                TreePrototype[] prototypes = terrainData.treePrototypes;
                Rect terrainArea = GetAreaOf(terrain);
                bool isRegrown = targetTerrains.Contains(terrain);
                foreach (TreeInstance instance in terrainData.treeInstances)
                {
                    GameObject prefab = prototypes[instance.prototypeIndex].prefab;
                    Vector2 position = new(terrainArea.x + instance.position.x * terrainArea.width, terrainArea.y + instance.position.z * terrainArea.height);
                    if (IsForestSpecies(prefab))
                    {
                        if (!isRegrown)
                        {
                            spacingGrid.AddSpot(position, settings.spacing);
                        }

                        continue;
                    }

                    if (!footprintOfPrefab.TryGetValue(prefab, out float footprint))
                    {
                        footprint = MeasureFootprint(prefab);
                        footprintOfPrefab.Add(prefab, footprint);
                    }

                    spacingGrid.AddSpot(position, footprint * instance.widthScale + settings.clearanceAroundObjects);
                }
            }
        }

        private bool IsForestSpecies(GameObject prefab)
        {
            return species.Exists(entry => entry.tree == prefab);
        }

        private static float MeasureFootprint(GameObject prefab)
        {
            Matrix4x4 worldToPrefab = prefab.transform.worldToLocalMatrix;
            float widestRadius = 0f;
            foreach (MeshFilter meshFilter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null) continue;

                Bounds bounds = meshFilter.sharedMesh.bounds;
                Matrix4x4 meshToPrefab = worldToPrefab * meshFilter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 cornerSign = new((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    Vector3 cornerInPrefab = meshToPrefab.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, cornerSign));
                    widestRadius = Mathf.Max(widestRadius, new Vector2(cornerInPrefab.x, cornerInPrefab.z).magnitude);
                }
            }

            return widestRadius;
        }

        private void ShuffleInPlace(int[] cellOrder)
        {
            for (int i = cellOrder.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (cellOrder[i], cellOrder[j]) = (cellOrder[j], cellOrder[i]);
            }
        }

        private void SowCell(int cellIndex)
        {
            Vector2 cellCenter = spacingGrid.GetCellCenter(cellIndex);
            Tile centerTile = FindTargetTileAt(cellCenter);
            if (centerTile == null) return;
            if (random.NextDouble() >= GetPlantingChance(centerTile, cellCenter)) return;

            for (int attempt = 0; attempt < TriesPerCell; attempt++)
            {
                Vector2 position = spacingGrid.GetRandomPointInCell(cellIndex, random);
                Tile tile = FindTargetTileAt(position);
                if (tile == null) continue;
                if (!spacingGrid.IsFreeAt(position)) continue;

                Vector2 uv = tile.GetUvAt(position);
                float slope = tile.TerrainData.GetSteepness(uv.x, uv.y);
                if (slope > settings.steepestSlope)
                {
                    rejectedAsSteep++;
                    continue;
                }

                float height = tile.TerrainData.GetInterpolatedHeight(uv.x, uv.y);
                Vector3 foot = new(position.x, tile.BaseHeight + height, position.y);
                if (IsNearWater(foot))
                {
                    rejectedNearWater++;
                    continue;
                }

                if (IsBlockedByObstacle(foot))
                {
                    rejectedAtObjects++;
                    continue;
                }

                PlantTree(tile, position, uv, height - TrunkRadius * Mathf.Tan(slope * Mathf.Deg2Rad));
                return;
            }
        }

        private Tile FindTargetTileAt(Vector2 position)
        {
            foreach (Tile tile in targetTiles)
            {
                if (tile.Area.Contains(position)) return tile;
            }

            return null;
        }

        private float GetPlantingChance(Tile tile, Vector2 position)
        {
            Vector2 uv = tile.GetUvAt(position);
            int side = tile.DensityMap.GetLength(0);
            float groundDensity = tile.DensityMap[Mathf.Min((int)(uv.y * side), side - 1), Mathf.Min((int)(uv.x * side), side - 1)];
            float height = tile.TerrainData.GetInterpolatedHeight(uv.x, uv.y);
            float belowTreeline = Mathf.Clamp01((settings.treelineHeight - tile.BaseHeight - height) / TreelineFade);
            return groundDensity * belowTreeline * Mathf.Lerp(1f, 2f * SampleGroveNoise(position, 0), settings.patchiness);
        }

        private float SampleGroveNoise(Vector2 position, int noiseLayer)
        {
            float noiseShift = noiseLayer * NoiseStride;
            return Mathf.InverseLerp(NoiseLow, NoiseHigh, Mathf.PerlinNoise(NoiseOrigin + noiseShift + position.x / settings.groveSize, NoiseOrigin - noiseShift + position.y / settings.groveSize));
        }

        private bool IsNearWater(Vector3 foot)
        {
            if (IsUnderWater(foot)) return true;

            for (int i = 0; i < ShoreProbes; i++)
            {
                float angle = 2f * Mathf.PI * i / ShoreProbes;
                Vector3 probe = new(foot.x + settings.distanceFromWater * Mathf.Cos(angle), 0f, foot.z + settings.distanceFromWater * Mathf.Sin(angle));
                int terrainIndex = FindTerrainIndexAt(probe);
                if (terrainIndex < 0) continue;

                Terrain terrain = allTerrains[terrainIndex];
                probe.y = terrain.transform.position.y + terrain.SampleHeight(probe);
                if (IsUnderWater(probe)) return true;
            }

            return false;
        }

        private int FindTerrainIndexAt(Vector3 point)
        {
            for (int i = 0; i < allTerrainAreas.Length; i++)
            {
                if (allTerrainAreas[i].Contains(new Vector2(point.x, point.z))) return i;
            }

            return -1;
        }

        private bool IsUnderWater(Vector3 point)
        {
            foreach (Water water in waters)
            {
                if (water.GetDepthAt(point) > 0f) return true;
            }

            return false;
        }

        private bool IsBlockedByObstacle(Vector3 foot)
        {
            Vector3 castStart = new(foot.x, ceilingHeight, foot.z);
            int hitCount = Physics.SphereCastNonAlloc(castStart, settings.clearanceAroundObjects, Vector3.down, obstacleHits, ceilingHeight - foot.y, settings.obstacles, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                if (obstacleHits[i].collider is not TerrainCollider) return true;
            }

            return false;
        }

        private void PlantTree(Tile tile, Vector2 position, Vector2 uv, float height)
        {
            int speciesIndex = PickSpeciesIndex(position);
            Vector2 scaleRange = species[speciesIndex].scaleRange;
            float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)random.NextDouble());
            byte gray = (byte)(255f * (1f - MostShade * (float)random.NextDouble()));
            tile.Trees.Add(new TreeInstance
            {
                position = new Vector3(uv.x, height / tile.TerrainData.size.y, uv.y),
                widthScale = scale,
                heightScale = scale,
                rotation = (float)(random.NextDouble() * Math.PI * 2.0),
                color = new Color32(gray, gray, gray, 255),
                lightmapColor = new Color32(255, 255, 255, 255),
                prototypeIndex = tile.PrototypeIndexOfSpecies[speciesIndex]
            });
            spacingGrid.AddSpot(position, settings.spacing);
            plantedTrees++;
        }

        private int PickSpeciesIndex(Vector2 position)
        {
            float totalWeight = 0f;
            for (int i = 0; i < species.Count; i++)
            {
                speciesWeights[i] = species[i].share * Mathf.Lerp(1f, 2f * SampleGroveNoise(position, i + 1), settings.patchiness);
                totalWeight += speciesWeights[i];
            }

            double roll = random.NextDouble() * totalWeight;
            int pickedIndex = 0;
            for (int i = 0; i < species.Count; i++)
            {
                if (speciesWeights[i] <= 0f) continue;

                pickedIndex = i;
                roll -= speciesWeights[i];
                if (roll < 0) break;
            }

            return pickedIndex;
        }

        private static Rect GetAreaOf(Terrain terrain)
        {
            Vector3 terrainCorner = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            return new Rect(terrainCorner.x, terrainCorner.z, terrainSize.x, terrainSize.z);
        }

        private static Rect GetBoundingArea(IEnumerable<Rect> areas)
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
