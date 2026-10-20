using System;
using System.Collections.Generic;
using Shooter.Game;
using Shooter.Infrastructure;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Editor
{
    public class GrowGrassTool : EditorWindow
    {
        private const int CellsPerPatch = 16;
        private const float DetailNoiseSpread = 0.1f;
        private const float DetailPositionJitter = 0.33f;
        private const float NoiseOrigin = 4096f;
        private const float NoiseStride = 37.3f;
        private const float NoiseLow = 0.3f;
        private const float NoiseHigh = 0.7f;
        private const float RayHeadroom = 10f;
        private const int RaysPerJob = 256;
        private static readonly Journal Log = Logs.Here();

        [Serializable]
        private class GrassPlant
        {
            public GameObject prefab;
            public float share = 1f;
            public Vector2 height = Vector2.one;
            public Vector2 width = Vector2.one;
        }

        [Serializable]
        private class GrassSet
        {
            public string name;
            public List<TerrainLayer> groundLayers = new();
            [Range(0f, 1f)] public float cover = 0.5f;
            public List<GrassPlant> plants = new();
        }

        private sealed class DetailLayer
        {
            public GrassSet GrassSet;
            public GrassPlant GrassPlant;
            public float ShareInSet;
            public Vector2 NoiseOffset;
            public int[] GroundChannels;
        }

        [SerializeField] private List<GrassSet> sets = new();
        [SerializeField] private float steepestSlope = 35f;
        [SerializeField] private float clumpSize = 15f;
        [SerializeField, Range(0f, 1f)] private float patchiness = 0.6f;
        [SerializeField] private float plantsPerSquareMeter = 4f;
        [SerializeField] private int cellsAlongTerrain = 1024;
        [SerializeField] private LayerMask obstacles = Physics.DefaultRaycastLayers;

        private string lastRunSummary;
        private SerializedObject serializedWindow;

        [MenuItem("Tools/Grow Grass")]
        private static void OpenWindow()
        {
            GetWindow<GrowGrassTool>("Grow Grass");
        }

        private void OnEnable()
        {
            serializedWindow = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serializedWindow.Update();
            DrawField(nameof(sets), "Sets, each on its ground layers", true);
            DrawField(nameof(steepestSlope), "Steepest slope, degrees");
            DrawField(nameof(clumpSize), "Clump size, m");
            DrawField(nameof(patchiness), "Patchiness");
            DrawField(nameof(plantsPerSquareMeter), "Plants per m² at full cover");
            DrawField(nameof(cellsAlongTerrain), "Grass cells along a terrain");
            DrawField(nameof(obstacles), "Obstacle layers");
            serializedWindow.ApplyModifiedProperties();

            EditorGUILayout.LabelField("Cover is how much of its ground a set fills, share is a plant's part of its set, height and width are " +
                                       "multipliers of the plant. Nothing grows under water, on slopes steeper than the limit or under anything " +
                                       "on the obstacle layers", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            var selectedTerrains = new List<Terrain>(Selection.GetFiltered<Terrain>(SelectionMode.Unfiltered));
            EditorGUILayout.LabelField(selectedTerrains.Count == 0 ? "Select the terrains to grow grass on" : $"{selectedTerrains.Count} selected terrains");

            using (new EditorGUI.DisabledScope(selectedTerrains.Count == 0))
            {
                if (GUILayout.Button("Grow grass"))
                {
                    GrowGrassOn(selectedTerrains);
                }
            }

            if (!string.IsNullOrEmpty(lastRunSummary))
            {
                EditorGUILayout.LabelField(lastRunSummary, EditorStyles.wordWrappedLabel);
            }
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void DrawField(string propertyName, string label, bool includeChildren = false)
        {
            EditorGUILayout.PropertyField(serializedWindow.FindProperty(propertyName), new GUIContent(label), includeChildren);
        }

        private void GrowGrassOn(List<Terrain> terrains)
        {
            if (EditorApplication.isPlaying)
            {
                Log.Warn("Grass grows in edit mode only, stop the play mode first");
                return;
            }

            List<DetailLayer> detailLayers = CreateDetailLayers();
            if (detailLayers.Count == 0)
            {
                Log.Warn("No grass set has a plant with a prefab and a share, nothing to grow");
                return;
            }

            DetailPrototype[] prototypes = CreateDetailPrototypes(detailLayers);
            if (prototypes == null) return;

            Water[] waters = Object.FindObjectsByType<Water>(FindObjectsInactive.Exclude);
            Physics.SyncTransforms();

            DateTime startTime = DateTime.Now;
            long grassyCells = 0;
            long bareCells = 0;
            try
            {
                for (int i = 0; i < terrains.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Grow Grass", terrains[i].name, (float)i / terrains.Count);
                    (long grassyOnTerrain, long bareOnTerrain) = GrowGrassOnTerrain(terrains[i], detailLayers, prototypes, waters);
                    grassyCells += grassyOnTerrain;
                    bareCells += bareOnTerrain;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            lastRunSummary = $"Grew grass on {terrains.Count} terrains in {(DateTime.Now - startTime).TotalSeconds:F1} s: {grassyCells} cells grassy, {bareCells} left bare under objects";
            Log.Info(lastRunSummary);
        }

        private List<DetailLayer> CreateDetailLayers()
        {
            var detailLayers = new List<DetailLayer>();
            foreach (GrassSet grassSet in sets)
            {
                float totalShare = 0f;
                foreach (GrassPlant plant in grassSet.plants)
                {
                    if (plant.prefab != null)
                    {
                        totalShare += plant.share;
                    }
                }

                if (totalShare <= 0f) continue;

                foreach (GrassPlant plant in grassSet.plants)
                {
                    if (plant.prefab == null) continue;

                    float noiseShift = (detailLayers.Count + 1) * NoiseStride;
                    detailLayers.Add(new DetailLayer { GrassSet = grassSet, GrassPlant = plant, ShareInSet = plant.share / totalShare, NoiseOffset = new Vector2(NoiseOrigin + noiseShift, NoiseOrigin - noiseShift) });
                }
            }

            return detailLayers;
        }

        private static DetailPrototype[] CreateDetailPrototypes(List<DetailLayer> detailLayers)
        {
            DetailPrototype[] prototypes = new DetailPrototype[detailLayers.Count];
            for (int i = 0; i < detailLayers.Count; i++)
            {
                GrassPlant plant = detailLayers[i].GrassPlant;
                DetailPrototype prototype = new()
                {
                    prototype = plant.prefab,
                    usePrototypeMesh = true,
                    useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit,
                    minHeight = plant.height.x,
                    maxHeight = plant.height.y,
                    minWidth = plant.width.x,
                    maxWidth = plant.width.y,
                    noiseSeed = i + 1,
                    noiseSpread = DetailNoiseSpread,
                    positionJitter = DetailPositionJitter
                };

                if (!prototype.Validate(out string error))
                {
                    Log.Warn($"Plant {plant.prefab.name} of set {detailLayers[i].GrassSet.name} cannot be a terrain detail: {error}");
                    return null;
                }

                prototypes[i] = prototype;
            }

            return prototypes;
        }

        private (long grassyCells, long bareCells) GrowGrassOnTerrain(Terrain terrain, List<DetailLayer> detailLayers, DetailPrototype[] prototypes, Water[] waters)
        {
            TerrainData terrainData = terrain.terrainData;
            terrainData.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            terrainData.detailPrototypes = prototypes;
            terrainData.SetDetailResolution(cellsAlongTerrain, CellsPerPatch);
            int mostPlantsPerCell = terrainData.maxDetailScatterPerRes;
            System.Random random = new(terrain.name.GetHashCode());

            TerrainLayer[] terrainLayers = terrainData.terrainLayers;
            foreach (DetailLayer detailLayer in detailLayers)
            {
                var groundChannels = new List<int>();
                foreach (TerrainLayer groundLayer in detailLayer.GrassSet.groundLayers)
                {
                    int channel = Array.IndexOf(terrainLayers, groundLayer);
                    if (channel >= 0)
                    {
                        groundChannels.Add(channel);
                    }
                }

                detailLayer.GroundChannels = groundChannels.ToArray();
            }

            int detailResolution = terrainData.detailResolution;
            int alphamapResolution = terrainData.alphamapResolution;
            float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, alphamapResolution, alphamapResolution);
            int[][,] detailMaps = new int[detailLayers.Count][,];
            for (int k = 0; k < detailLayers.Count; k++)
            {
                detailMaps[k] = new int[detailResolution, detailResolution];
            }

            Vector3 terrainCorner = terrain.transform.position;
            Vector3 terrainSize = terrainData.size;
            float cellArea = terrainSize.x / detailResolution * (terrainSize.z / detailResolution);
            var grassyCellIndices = new List<int>();
            for (int y = 0; y < detailResolution; y++)
            {
                for (int x = 0; x < detailResolution; x++)
                {
                    float u = (x + 0.5f) / detailResolution;
                    float v = (y + 0.5f) / detailResolution;
                    if (terrainData.GetSteepness(u, v) > steepestSlope) continue;

                    Vector3 point = new(terrainCorner.x + u * terrainSize.x, terrainCorner.y + terrainData.GetInterpolatedHeight(u, v), terrainCorner.z + v * terrainSize.z);
                    if (IsUnderWater(point, waters)) continue;

                    int alphamapX = Mathf.Min((int)(u * alphamapResolution), alphamapResolution - 1);
                    int alphamapY = Mathf.Min((int)(v * alphamapResolution), alphamapResolution - 1);
                    bool isGrassy = false;
                    for (int k = 0; k < detailLayers.Count; k++)
                    {
                        DetailLayer detailLayer = detailLayers[k];
                        float groundWeight = 0f;
                        foreach (int channel in detailLayer.GroundChannels)
                        {
                            groundWeight += alphamaps[alphamapY, alphamapX, channel];
                        }

                        if (groundWeight <= 0f) continue;

                        float noise = Mathf.InverseLerp(NoiseLow, NoiseHigh, Mathf.PerlinNoise(point.x / clumpSize + detailLayer.NoiseOffset.x, point.z / clumpSize + detailLayer.NoiseOffset.y));
                        float amount = groundWeight * detailLayer.GrassSet.cover * detailLayer.ShareInSet * Mathf.Lerp(1f, 2f * noise, patchiness);
                        float expectedPlants = amount * plantsPerSquareMeter * cellArea;
                        int plantCount = Mathf.Min((int)expectedPlants + (random.NextDouble() < expectedPlants % 1f ? 1 : 0), mostPlantsPerCell);
                        if (plantCount == 0) continue;

                        detailMaps[k][y, x] = plantCount;
                        isGrassy = true;
                    }

                    if (isGrassy)
                    {
                        grassyCellIndices.Add(y * detailResolution + x);
                    }
                }
            }

            long clearedCells = ClearGrassUnderObjects(terrain, grassyCellIndices, detailMaps, detailResolution);
            for (int k = 0; k < detailLayers.Count; k++)
            {
                terrainData.SetDetailLayer(0, 0, k, detailMaps[k]);
            }

            EditorUtility.SetDirty(terrainData);
            return (grassyCellIndices.Count - clearedCells, clearedCells);
        }

        private static bool IsUnderWater(Vector3 point, Water[] waters)
        {
            foreach (Water water in waters)
            {
                if (water.GetDepthAt(point) > 0f) return true;
            }

            return false;
        }

        private long ClearGrassUnderObjects(Terrain terrain, List<int> grassyCellIndices, int[][,] detailMaps, int detailResolution)
        {
            if (grassyCellIndices.Count == 0) return 0;

            Vector3 terrainCorner = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            float rayStartHeight = terrainCorner.y + terrainSize.y + RayHeadroom;
            float rayLength = terrainSize.y + 2f * RayHeadroom;
            QueryParameters query = new(obstacles, false, QueryTriggerInteraction.Ignore, false);
            var commands = new NativeArray<RaycastCommand>(grassyCellIndices.Count, Allocator.TempJob);
            var hits = new NativeArray<RaycastHit>(grassyCellIndices.Count, Allocator.TempJob);
            try
            {
                for (int i = 0; i < grassyCellIndices.Count; i++)
                {
                    int x = grassyCellIndices[i] % detailResolution;
                    int y = grassyCellIndices[i] / detailResolution;
                    Vector3 rayStart = new(terrainCorner.x + (x + 0.5f) / detailResolution * terrainSize.x, rayStartHeight, terrainCorner.z + (y + 0.5f) / detailResolution * terrainSize.z);
                    commands[i] = new RaycastCommand(rayStart, Vector3.down, query, rayLength);
                }

                RaycastCommand.ScheduleBatch(commands, hits, RaysPerJob, 1).Complete();

                long clearedCells = 0;
                for (int i = 0; i < grassyCellIndices.Count; i++)
                {
                    Collider hitCollider = hits[i].collider;
                    if (hitCollider == null || hitCollider is TerrainCollider) continue;

                    int x = grassyCellIndices[i] % detailResolution;
                    int y = grassyCellIndices[i] / detailResolution;
                    foreach (int[,] detailMap in detailMaps)
                    {
                        detailMap[y, x] = 0;
                    }

                    clearedCells++;
                }

                return clearedCells;
            }
            finally
            {
                commands.Dispose();
                hits.Dispose();
            }
        }
    }
}
