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
        private const float Spread = 0.1f;
        private const float Jitter = 0.33f;
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
            public GrassSet Set;
            public GrassPlant Plant;
            public float Share;
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

        private string result;
        private SerializedObject serialized;

        [MenuItem("Tools/Grow Grass")]
        private static void Open()
        {
            GetWindow<GrowGrassTool>("Grow Grass");
        }

        private void OnEnable()
        {
            serialized = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serialized.Update();
            Field(nameof(sets), "Sets, each on its ground layers", true);
            Field(nameof(steepestSlope), "Steepest slope, degrees");
            Field(nameof(clumpSize), "Clump size, m");
            Field(nameof(patchiness), "Patchiness");
            Field(nameof(plantsPerSquareMeter), "Plants per m² at full cover");
            Field(nameof(cellsAlongTerrain), "Grass cells along a terrain");
            Field(nameof(obstacles), "Obstacle layers");
            serialized.ApplyModifiedProperties();

            EditorGUILayout.LabelField("Cover is how much of its ground a set fills, share is a plant's part of its set, height and width are " +
                                       "multipliers of the plant. Nothing grows under water, on slopes steeper than the limit or under anything " +
                                       "on the obstacle layers", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            List<Terrain> targets = Landscape.Targets();
            EditorGUILayout.LabelField(Landscape.Selected() ? $"{targets.Count} selected terrains" : $"{targets.Count} terrains of the landscape");

            if (GUILayout.Button("Grow grass"))
            {
                Grow(targets);
            }

            if (!string.IsNullOrEmpty(result))
            {
                EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
            }
        }

        private void Field(string name, string label, bool children = false)
        {
            EditorGUILayout.PropertyField(serialized.FindProperty(name), new GUIContent(label), children);
        }

        private void Grow(List<Terrain> targets)
        {
            if (EditorApplication.isPlaying)
            {
                Log.Warn("Grass grows in edit mode only, stop the play mode first");
                return;
            }

            if (targets.Count == 0)
            {
                Log.Warn("Scene has no terrain to grow grass on");
                return;
            }

            List<DetailLayer> layers = DetailLayers();
            if (layers.Count == 0)
            {
                Log.Warn("No grass set has a plant with a prefab and a share, nothing to grow");
                return;
            }

            DetailPrototype[] prototypes = Prototypes(layers);
            if (prototypes == null) return;

            Water[] waters = Object.FindObjectsByType<Water>(FindObjectsInactive.Exclude);
            Physics.SyncTransforms();

            var started = DateTime.Now;
            long grassy = 0;
            long bare = 0;
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Grow Grass", targets[i].name, (float)i / targets.Count);
                    (long grassyHere, long bareHere) = GrowOn(targets[i], layers, prototypes, waters);
                    grassy += grassyHere;
                    bare += bareHere;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            result = $"Grew grass on {targets.Count} terrains in {(DateTime.Now - started).TotalSeconds:F1} s: {grassy} cells grassy, {bare} left bare under objects";
            Log.Info(result);
        }

        private List<DetailLayer> DetailLayers()
        {
            var layers = new List<DetailLayer>();
            foreach (GrassSet set in sets)
            {
                float total = 0f;
                foreach (GrassPlant plant in set.plants)
                {
                    if (plant.prefab != null)
                    {
                        total += plant.share;
                    }
                }

                if (total <= 0f) continue;

                foreach (GrassPlant plant in set.plants)
                {
                    if (plant.prefab == null) continue;

                    float stride = (layers.Count + 1) * NoiseStride;
                    layers.Add(new DetailLayer { Set = set, Plant = plant, Share = plant.share / total, NoiseOffset = new Vector2(NoiseOrigin + stride, NoiseOrigin - stride) });
                }
            }

            return layers;
        }

        private static DetailPrototype[] Prototypes(List<DetailLayer> layers)
        {
            var prototypes = new DetailPrototype[layers.Count];
            for (int i = 0; i < layers.Count; i++)
            {
                GrassPlant plant = layers[i].Plant;
                var prototype = new DetailPrototype
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
                    noiseSpread = Spread,
                    positionJitter = Jitter
                };

                if (!prototype.Validate(out string error))
                {
                    Log.Warn($"Plant {plant.prefab.name} of set {layers[i].Set.name} cannot be a terrain detail: {error}");
                    return null;
                }

                prototypes[i] = prototype;
            }

            return prototypes;
        }

        private (long grassy, long bare) GrowOn(Terrain terrain, List<DetailLayer> layers, DetailPrototype[] prototypes, Water[] waters)
        {
            TerrainData data = terrain.terrainData;
            data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            data.detailPrototypes = prototypes;
            data.SetDetailResolution(cellsAlongTerrain, CellsPerPatch);
            int mostPerCell = data.maxDetailScatterPerRes;
            var random = new System.Random(terrain.name.GetHashCode());

            TerrainLayer[] palette = data.terrainLayers;
            foreach (DetailLayer layer in layers)
            {
                var channels = new List<int>();
                foreach (TerrainLayer ground in layer.Set.groundLayers)
                {
                    int index = Array.IndexOf(palette, ground);
                    if (index >= 0)
                    {
                        channels.Add(index);
                    }
                }

                layer.GroundChannels = channels.ToArray();
            }

            int side = data.detailResolution;
            int alphaSide = data.alphamapResolution;
            float[,,] alphamaps = data.GetAlphamaps(0, 0, alphaSide, alphaSide);
            var maps = new int[layers.Count][,];
            for (int k = 0; k < layers.Count; k++)
            {
                maps[k] = new int[side, side];
            }

            Vector3 corner = terrain.transform.position;
            Vector3 size = data.size;
            float cellArea = size.x / side * (size.z / side);
            var grassyCells = new List<int>();
            for (int y = 0; y < side; y++)
            {
                for (int x = 0; x < side; x++)
                {
                    float u = (x + 0.5f) / side;
                    float v = (y + 0.5f) / side;
                    if (data.GetSteepness(u, v) > steepestSlope) continue;

                    var point = new Vector3(corner.x + u * size.x, corner.y + data.GetInterpolatedHeight(u, v), corner.z + v * size.z);
                    if (Submerged(point, waters)) continue;

                    int ax = Mathf.Min((int)(u * alphaSide), alphaSide - 1);
                    int ay = Mathf.Min((int)(v * alphaSide), alphaSide - 1);
                    bool grassy = false;
                    for (int k = 0; k < layers.Count; k++)
                    {
                        DetailLayer layer = layers[k];
                        float weight = 0f;
                        foreach (int channel in layer.GroundChannels)
                        {
                            weight += alphamaps[ay, ax, channel];
                        }

                        if (weight <= 0f) continue;

                        float noise = Mathf.InverseLerp(NoiseLow, NoiseHigh, Mathf.PerlinNoise(point.x / clumpSize + layer.NoiseOffset.x, point.z / clumpSize + layer.NoiseOffset.y));
                        float amount = weight * layer.Set.cover * layer.Share * Mathf.Lerp(1f, 2f * noise, patchiness);
                        float expected = amount * plantsPerSquareMeter * cellArea;
                        int count = Mathf.Min((int)expected + (random.NextDouble() < expected % 1f ? 1 : 0), mostPerCell);
                        if (count == 0) continue;

                        maps[k][y, x] = count;
                        grassy = true;
                    }

                    if (grassy)
                    {
                        grassyCells.Add(y * side + x);
                    }
                }
            }

            long bare = ClearUnderObjects(terrain, grassyCells, maps, side);
            for (int k = 0; k < layers.Count; k++)
            {
                data.SetDetailLayer(0, 0, k, maps[k]);
            }

            EditorUtility.SetDirty(data);
            return (grassyCells.Count - bare, bare);
        }

        private static bool Submerged(Vector3 point, Water[] waters)
        {
            foreach (Water water in waters)
            {
                if (water.DepthAt(point) > 0f) return true;
            }

            return false;
        }

        private long ClearUnderObjects(Terrain terrain, List<int> grassyCells, int[][,] maps, int side)
        {
            if (grassyCells.Count == 0) return 0;

            Vector3 corner = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            float top = corner.y + size.y + RayHeadroom;
            float reach = size.y + 2f * RayHeadroom;
            var query = new QueryParameters(obstacles, false, QueryTriggerInteraction.Ignore, false);
            var commands = new NativeArray<RaycastCommand>(grassyCells.Count, Allocator.TempJob);
            var hits = new NativeArray<RaycastHit>(grassyCells.Count, Allocator.TempJob);
            try
            {
                for (int i = 0; i < grassyCells.Count; i++)
                {
                    int x = grassyCells[i] % side;
                    int y = grassyCells[i] / side;
                    var origin = new Vector3(corner.x + (x + 0.5f) / side * size.x, top, corner.z + (y + 0.5f) / side * size.z);
                    commands[i] = new RaycastCommand(origin, Vector3.down, query, reach);
                }

                RaycastCommand.ScheduleBatch(commands, hits, RaysPerJob, 1).Complete();

                long cleared = 0;
                for (int i = 0; i < grassyCells.Count; i++)
                {
                    Collider first = hits[i].collider;
                    if (first == null || first is TerrainCollider) continue;

                    int x = grassyCells[i] % side;
                    int y = grassyCells[i] / side;
                    foreach (int[,] map in maps)
                    {
                        map[y, x] = 0;
                    }

                    cleared++;
                }

                return cleared;
            }
            finally
            {
                commands.Dispose();
                hits.Dispose();
            }
        }
    }
}
