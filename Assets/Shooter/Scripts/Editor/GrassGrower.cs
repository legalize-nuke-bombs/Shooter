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
    public class GrassGrower : EditorWindow
    {
        private const string LayerFolder = "Assets/Shooter/Data/Environment/Terrain/Layers/";
        private const string PlantFolder = "Assets/Shooter/Data/Environment/Props/Nature/";
        private const int CellsPerPatch = 32;
        private const float Spread = 0.1f;
        private const float Jitter = 0.33f;
        private const float NoiseOrigin = 4096f;
        private const float NoiseStride = 37.3f;
        private const float NoiseLow = 0.3f;
        private const float NoiseHigh = 0.7f;
        private const float RayHeadroom = 10f;
        private const int RaysPerJob = 256;
        private static readonly string[] Unrooted = { "Character", "Hitbox", "FirstPerson", "ReflectionOnly", "Item" };
        private static readonly Journal Log = Logs.Here();

        [Serializable]
        private class Blade
        {
            public GameObject plant;
            public float share = 1f;
            public Vector2 height = Vector2.one;
            public Vector2 width = Vector2.one;
        }

        [Serializable]
        private class Set
        {
            public string name;
            public List<TerrainLayer> ground = new();
            [Range(0f, 1f)] public float cover = 0.5f;
            public List<Blade> blades = new();
        }

        private sealed class Kind
        {
            public Set Set;
            public Blade Blade;
            public float Share;
            public Vector2 Offset;
            public int[] Layers;
        }

        [SerializeField] private List<Set> sets = new();
        [SerializeField] private float steepest = 35f;
        [SerializeField] private float clump = 15f;
        [SerializeField, Range(0f, 1f)] private float patchiness = 0.6f;
        [SerializeField] private float plants = 4f;
        [SerializeField] private int cells = 1024;

        private string result;
        private SerializedObject serialized;

        private void OnEnable()
        {
            if (sets.Count == 0) Defaults();

            serialized = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(sets)), new GUIContent("Sets, each on its ground layers"), true);
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(steepest)), new GUIContent("Steepest slope, degrees"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(clump)), new GUIContent("Clump size, m"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(patchiness)), new GUIContent("Patchiness"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(plants)), new GUIContent("Plants per m² at full cover"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(cells)), new GUIContent("Grass cells along a terrain"));
            serialized.ApplyModifiedProperties();

            EditorGUILayout.LabelField("Cover is how much of its ground a set fills, share is a blade's part of its set, height and width are " +
                                       "multipliers of the plant. Nothing grows under water, on slopes steeper than the limit or under anything " +
                                       "standing on the terrain", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            List<Terrain> targets = Landscape.Targets();
            EditorGUILayout.LabelField(Landscape.Selected() ? $"{targets.Count} selected terrains" : $"{targets.Count} terrains of the landscape");

            if (GUILayout.Button("Grow grass")) Grow();
            if (GUILayout.Button("Defaults"))
            {
                Undo.RecordObject(this, "Grass grower defaults");
                Defaults();
                serialized.Update();
            }

            if (!string.IsNullOrEmpty(result)) EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
        }

        [MenuItem("Tools/Grow Grass")]
        private static void Open()
        {
            GetWindow<GrassGrower>("Grow Grass");
        }

        private void Defaults()
        {
            steepest = 35f;
            clump = 15f;
            patchiness = 0.6f;
            plants = 4f;
            cells = 1024;
            sets = new List<Set>
            {
                Make("Meadow", 0.6f, new[] { "Grass_A", "Grass_B" },
                    Of("Grass/Grass_A", 0.35f, 2f, 3f), Of("Grass/Grass_B", 0.25f, 1f, 2.5f), Of("Grass/Grass_C", 0.25f, 1f, 2f),
                    Of("Heather/Heather_B", 0.05f, 1f, 2f), Of("Heather/Heather_A", 0.03f, 1f, 2f), Of("Bushes/Shrub", 0.04f, 1f, 2f),
                    Of("Bushes/Bush_A", 0.015f, 1f, 2f), Of("Bushes/Bush_B", 0.015f, 1f, 1.5f)),
                Make("Forest floor", 0.5f, new[] { "Grass_Moss_A" },
                    Of("Ferns/Fern_A", 0.3f, 2f, 3f), Of("Ferns/Fern_B", 0.25f, 2f, 3f), Of("Ferns/Fern_C", 0.2f, 2f, 3f),
                    Of("Bushes/Bush_Twig", 0.1f, 2f, 3f), Of("Grass/Grass_B", 0.15f, 1f, 1.5f)),
                Make("Fireweed", 0.7f, new[] { "Heather_A" },
                    Of("Heather/Heather_B", 0.4f, 1f, 2f), Of("Heather/Heather_A", 0.3f, 1f, 2f), Of("Grass/Grass_C", 0.3f, 1f, 2f)),
                Make("Path", 0.25f, new[] { "Grass_Soil_A" },
                    Of("Grass/Grass_B", 0.6f, 0.6f, 1f), Of("Grass/GrassDry_C", 0.4f, 0.8f, 1.2f)),
                Make("Pebbles", 0.1f, new[] { "Pebbles_B" },
                    Of("Grass/GrassDry_B", 0.5f, 1f, 1.5f), Of("Grass/GrassDry_D", 0.5f, 0.8f, 1.2f)),
                Make("Shore", 0.5f, new[] { "Tidal_Pools_B" },
                    Of("Grass/Grass_A", 0.6f, 2f, 3f), Of("Grass/Grass_D", 0.4f, 1f, 1.5f))
            };
        }

        private static Set Make(string name, float cover, string[] ground, params Blade[] blades)
        {
            var set = new Set { name = name, cover = cover, blades = new List<Blade>(blades) };
            foreach (string layer in ground)
            {
                set.ground.Add(AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerFolder + layer + ".terrainlayer"));
            }

            return set;
        }

        private static Blade Of(string plant, float share, float shortest, float tallest)
        {
            return new Blade
            {
                plant = AssetDatabase.LoadAssetAtPath<GameObject>(PlantFolder + plant + ".prefab"),
                share = share,
                height = new Vector2(shortest, tallest),
                width = new Vector2(shortest, tallest)
            };
        }

        private void Grow()
        {
            if (EditorApplication.isPlaying)
            {
                Log.Error("Grass grows in edit mode only, stop the play mode first");
                return;
            }

            List<Terrain> targets = Landscape.Targets();
            if (targets.Count == 0)
            {
                Log.Error("Scene has no terrain to grow grass on");
                return;
            }

            List<Kind> kinds = Kinds();
            if (kinds.Count == 0)
            {
                Log.Error("Grass grower has no blade with a plant, nothing grown");
                return;
            }

            DetailPrototype[] prototypes = Prototypes(kinds);
            if (prototypes == null) return;

            Water[] waters = Object.FindObjectsByType<Water>(FindObjectsInactive.Exclude);
            int solid = Physics.DefaultRaycastLayers & ~LayerMask.GetMask(Unrooted);
            Physics.SyncTransforms();

            var started = DateTime.Now;
            long green = 0;
            long shaded = 0;
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Grow Grass", targets[i].name, (float)i / targets.Count);
                    (long grown, long under) = Grow(targets[i], kinds, prototypes, waters, solid);
                    green += grown;
                    shaded += under;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            result = $"Grew grass on {targets.Count} terrains in {(DateTime.Now - started).TotalSeconds:F1} s: {green} cells green, " +
                     $"{shaded} left bare under objects";
            Log.Info(result);
        }

        private List<Kind> Kinds()
        {
            var kinds = new List<Kind>();
            foreach (Set set in sets)
            {
                float total = 0f;
                foreach (Blade blade in set.blades)
                {
                    if (blade.plant != null)
                    {
                        total += blade.share;
                    }
                }

                if (total <= 0f) continue;

                foreach (Blade blade in set.blades)
                {
                    if (blade.plant == null) continue;

                    float stride = (kinds.Count + 1) * NoiseStride;
                    kinds.Add(new Kind { Set = set, Blade = blade, Share = blade.share / total, Offset = new Vector2(NoiseOrigin + stride, NoiseOrigin - stride) });
                }
            }

            return kinds;
        }

        private static DetailPrototype[] Prototypes(List<Kind> kinds)
        {
            var prototypes = new DetailPrototype[kinds.Count];
            for (int i = 0; i < kinds.Count; i++)
            {
                Blade blade = kinds[i].Blade;
                var prototype = new DetailPrototype
                {
                    prototype = blade.plant,
                    usePrototypeMesh = true,
                    useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit,
                    minHeight = blade.height.x,
                    maxHeight = blade.height.y,
                    minWidth = blade.width.x,
                    maxWidth = blade.width.y,
                    noiseSeed = i + 1,
                    noiseSpread = Spread,
                    positionJitter = Jitter
                };

                if (!prototype.Validate(out string error))
                {
                    Log.Error($"Grass grower cannot plant {blade.plant.name} of {kinds[i].Set.name}: {error}");
                    return null;
                }

                prototypes[i] = prototype;
            }

            return prototypes;
        }

        private (long, long) Grow(Terrain terrain, List<Kind> kinds, DetailPrototype[] prototypes, Water[] waters, int solid)
        {
            TerrainData data = terrain.terrainData;
            data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            data.detailPrototypes = prototypes;
            data.SetDetailResolution(cells, CellsPerPatch);
            int most = data.maxDetailScatterPerRes;
            var dither = new System.Random(terrain.name.GetHashCode());

            TerrainLayer[] palette = data.terrainLayers;
            foreach (Kind kind in kinds)
            {
                var layers = new List<int>();
                foreach (TerrainLayer layer in kind.Set.ground)
                {
                    int index = Array.IndexOf(palette, layer);
                    if (index >= 0)
                    {
                        layers.Add(index);
                    }
                }

                kind.Layers = layers.ToArray();
            }

            int side = data.detailResolution;
            int alphaSide = data.alphamapResolution;
            float[,,] ground = data.GetAlphamaps(0, 0, alphaSide, alphaSide);
            var maps = new int[kinds.Count][,];
            for (int k = 0; k < kinds.Count; k++)
            {
                maps[k] = new int[side, side];
            }

            Vector3 corner = terrain.transform.position;
            Vector3 size = data.size;
            float cellArea = size.x / side * (size.z / side);
            var green = new List<int>();
            for (int y = 0; y < side; y++)
            {
                for (int x = 0; x < side; x++)
                {
                    float u = (x + 0.5f) / side;
                    float v = (y + 0.5f) / side;
                    if (data.GetSteepness(u, v) > steepest) continue;

                    var point = new Vector3(corner.x + u * size.x, corner.y + data.GetInterpolatedHeight(u, v), corner.z + v * size.z);
                    if (Submerged(point, waters)) continue;

                    int ax = Mathf.Min((int)(u * alphaSide), alphaSide - 1);
                    int ay = Mathf.Min((int)(v * alphaSide), alphaSide - 1);
                    bool any = false;
                    for (int k = 0; k < kinds.Count; k++)
                    {
                        Kind kind = kinds[k];
                        float weight = 0f;
                        foreach (int layer in kind.Layers)
                        {
                            weight += ground[ay, ax, layer];
                        }

                        if (weight <= 0f) continue;

                        float noise = Mathf.InverseLerp(NoiseLow, NoiseHigh, Mathf.PerlinNoise(point.x / clump + kind.Offset.x, point.z / clump + kind.Offset.y));
                        float amount = weight * kind.Set.cover * kind.Share * Mathf.Lerp(1f, 2f * noise, patchiness);
                        float expected = amount * plants * cellArea;
                        int value = Mathf.Min((int)expected + (dither.NextDouble() < expected % 1f ? 1 : 0), most);
                        if (value == 0) continue;

                        maps[k][y, x] = value;
                        any = true;
                    }

                    if (any)
                    {
                        green.Add(y * side + x);
                    }
                }
            }

            long under = Uproot(terrain, green, maps, side, solid);
            for (int k = 0; k < kinds.Count; k++)
            {
                data.SetDetailLayer(0, 0, k, maps[k]);
            }

            EditorUtility.SetDirty(data);
            return (green.Count - under, under);
        }

        private static bool Submerged(Vector3 point, Water[] waters)
        {
            foreach (Water water in waters)
            {
                if (water.DepthAt(point) > 0f) return true;
            }

            return false;
        }

        private static long Uproot(Terrain terrain, List<int> green, int[][,] maps, int side, int solid)
        {
            if (green.Count == 0) return 0;

            Vector3 corner = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            float top = corner.y + size.y + RayHeadroom;
            float reach = size.y + 2f * RayHeadroom;
            var query = new QueryParameters(solid, false, QueryTriggerInteraction.Ignore, false);
            var commands = new NativeArray<RaycastCommand>(green.Count, Allocator.TempJob);
            var hits = new NativeArray<RaycastHit>(green.Count, Allocator.TempJob);
            try
            {
                for (int i = 0; i < green.Count; i++)
                {
                    int x = green[i] % side;
                    int y = green[i] / side;
                    var origin = new Vector3(corner.x + (x + 0.5f) / side * size.x, top, corner.z + (y + 0.5f) / side * size.z);
                    commands[i] = new RaycastCommand(origin, Vector3.down, query, reach);
                }

                RaycastCommand.ScheduleBatch(commands, hits, RaysPerJob, 1).Complete();

                long uprooted = 0;
                for (int i = 0; i < green.Count; i++)
                {
                    Collider first = hits[i].collider;
                    if (first == null || first is TerrainCollider) continue;

                    int x = green[i] % side;
                    int y = green[i] / side;
                    foreach (int[,] map in maps)
                    {
                        map[y, x] = 0;
                    }

                    uprooted++;
                }

                return uprooted;
            }
            finally
            {
                commands.Dispose();
                hits.Dispose();
            }
        }
    }
}
