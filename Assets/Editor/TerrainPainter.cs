using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Logging;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editing
{
    public class TerrainPainter : EditorWindow
    {
        private const string LayerFolder = "Assets/Game/World/Data/Terrain/Layers/";
        private const string LandscapeRoot = "Landscape";
        private const string UndoName = "Paint terrain layers";
        private static readonly Journal Log = Logs.Here();

        private enum Measure
        {
            Slope,
            Height,
            Hollow
        }

        [Serializable]
        private class Rule
        {
            public TerrainLayer layer;
            public Measure measure;
            public float from;
            public float to;
            public float blend;
            [Range(0f, 1f)] public float strength = 1f;
        }

        private sealed class Tile
        {
            public Vector3 Corner;
            public float[,] Heights;
            public int Resolution;
            public Vector3 Size;
        }

        // Heights of every terrain around, so slopes and hollows read across tile seams
        private sealed class Ground
        {
            private readonly List<Tile> tiles = new();
            private Tile last;

            public Ground(IEnumerable<Terrain> terrains)
            {
                foreach (Terrain terrain in terrains)
                {
                    TerrainData data = terrain.terrainData;
                    int resolution = data.heightmapResolution;
                    tiles.Add(new Tile
                    {
                        Corner = terrain.transform.position,
                        Heights = data.GetHeights(0, 0, resolution, resolution),
                        Resolution = resolution,
                        Size = data.size
                    });
                }
            }

            public float Height(float x, float z)
            {
                if (last == null || !Inside(last, x, z))
                {
                    Tile found = null;
                    foreach (Tile tile in tiles)
                        if (Inside(tile, x, z))
                        {
                            found = tile;
                            break;
                        }

                    last = found ?? last ?? tiles[0];
                }

                return Sample(last, x, z);
            }

            private static bool Inside(Tile tile, float x, float z)
            {
                return x >= tile.Corner.x && x <= tile.Corner.x + tile.Size.x &&
                       z >= tile.Corner.z && z <= tile.Corner.z + tile.Size.z;
            }

            private static float Sample(Tile tile, float x, float z)
            {
                int top = tile.Resolution - 1;
                float u = Mathf.Clamp01((x - tile.Corner.x) / tile.Size.x) * top;
                float v = Mathf.Clamp01((z - tile.Corner.z) / tile.Size.z) * top;
                int column = Mathf.Min((int)u, top - 1);
                int row = Mathf.Min((int)v, top - 1);
                float across = u - column;
                float along = v - row;

                float[,] h = tile.Heights;
                float near = h[row, column] + (h[row, column + 1] - h[row, column]) * across;
                float far = h[row + 1, column] + (h[row + 1, column + 1] - h[row + 1, column]) * across;

                return (near + (far - near) * along) * tile.Size.y + tile.Corner.y;
            }
        }

        [SerializeField] private TerrainLayer background;
        [SerializeField] private float hollowRadius = 12f;
        [SerializeField] private List<Rule> rules = new();
        [SerializeField] private TerrainLayer marker;

        private string result;
        private SerializedObject serialized;

        private void OnEnable()
        {
            if (background == null && rules.Count == 0) Defaults();

            serialized = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(background)), new GUIContent("Background"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(hollowRadius)), new GUIContent("Hollow radius, m"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(rules)), new GUIContent("Rules, later over earlier"), true);
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(marker)), new GUIContent("Only where painted with"));
            serialized.ApplyModifiedProperties();

            EditorGUILayout.LabelField("Slope in degrees, height in world metres, hollow in metres below the ground around",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField("With a marker layer set, only the ground painted with it is repainted, the rest stays as it is",
                EditorStyles.miniLabel);

            EditorGUILayout.Space();
            List<Terrain> targets = Targets(out _);
            EditorGUILayout.LabelField(Selection.gameObjects.Length > 0 && targets.Count > 0 && HasSelectedTerrain()
                ? $"{targets.Count} selected terrains"
                : $"{targets.Count} terrains of the landscape");

            if (GUILayout.Button(marker == null ? "Paint everything" : $"Paint over {marker.name}")) Paint();
            if (GUILayout.Button("Defaults"))
            {
                Undo.RecordObject(this, "Terrain painter defaults");
                Defaults();
                serialized.Update();
            }

            if (!string.IsNullOrEmpty(result)) EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
        }

        [MenuItem("Tools/Paint Terrain Layers")]
        private static void Open()
        {
            GetWindow<TerrainPainter>("Paint Terrain Layers");
        }

        private void Defaults()
        {
            background = Load("Grass_Moss_A");
            hollowRadius = 12f;
            rules = new List<Rule>
            {
                new() { layer = Load("Grass_Soil_A"), measure = Measure.Hollow, from = 0.8f, to = 1000f, blend = 0.8f, strength = 0.6f },
                new() { layer = Load("Black_Sand_Rocks_B"), measure = Measure.Slope, from = 30f, to = 90f, blend = 6f, strength = 1f },
                new() { layer = Load("Cliff_Mossy_E"), measure = Measure.Slope, from = 42f, to = 90f, blend = 6f, strength = 1f }
            };
        }

        private static TerrainLayer Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerFolder + name + ".terrainlayer");
        }

        private static bool HasSelectedTerrain()
        {
            foreach (GameObject selected in Selection.gameObjects)
                if (selected.GetComponent<Terrain>() != null)
                    return true;

            return false;
        }

        // Selected terrains when any are selected, otherwise the whole landscape; heights are always read from all of it
        private static List<Terrain> Targets(out List<Terrain> around)
        {
            around = new List<Terrain>();

            GameObject root = GameObject.Find(LandscapeRoot);
            if (root != null) around.AddRange(root.GetComponentsInChildren<Terrain>());
            if (around.Count == 0) around.AddRange(Terrain.activeTerrains);

            var chosen = new List<Terrain>();
            foreach (GameObject selected in Selection.gameObjects)
            {
                Terrain terrain = selected.GetComponent<Terrain>();
                if (terrain == null || chosen.Contains(terrain)) continue;

                chosen.Add(terrain);
                if (!around.Contains(terrain)) around.Add(terrain);
            }

            return chosen.Count > 0 ? chosen : new List<Terrain>(around);
        }

        private void Paint()
        {
            List<Terrain> targets = Targets(out List<Terrain> around);
            if (targets.Count == 0)
            {
                Log.Error("Scene has no terrain to paint");
                return;
            }

            if (background == null)
            {
                Log.Error("Terrain painter has no background layer, nothing painted");
                return;
            }

            var started = DateTime.Now;
            var ground = new Ground(around);
            var shares = new Dictionary<TerrainLayer, double>();
            double texels = 0d;
            int painted = 0;

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Paint Terrain Layers", targets[i].name, (float)i / targets.Count);
                    double covered = Paint(targets[i], ground, shares);
                    if (covered <= 0d) continue;

                    texels += covered;
                    painted++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(group);
            }

            if (painted == 0)
            {
                result = marker == null ? "Nothing painted" : $"Nothing painted: no terrain here is painted with {marker.name}";
                Log.Info(result);
                return;
            }

            var summary = new StringBuilder($"Painted {painted} terrains in {(DateTime.Now - started).TotalSeconds:F1} s:");
            foreach (KeyValuePair<TerrainLayer, double> share in shares)
                summary.Append($" {share.Key.name} {share.Value / texels:P0}");

            result = summary.ToString();
            Log.Info(result);
        }

        // With a marker the old map stays wherever the marker is absent, and the rules take the marker's share of every texel it touches
        private double Paint(Terrain terrain, Ground ground, Dictionary<TerrainLayer, double> shares)
        {
            TerrainData data = terrain.terrainData;
            if (marker != null && !Marked(data)) return 0d;

            Undo.RegisterCompleteObjectUndo(data, UndoName);
            Undo.RegisterCompleteObjectUndo(data.alphamapTextures, UndoName);

            int[] channels = Palette(data);
            TerrainLayer[] palette = data.terrainLayers;
            int layers = palette.Length;
            int resolution = data.alphamapResolution;
            int marked = marker == null ? -1 : Array.IndexOf(palette, marker);
            float[,,] before = marked < 0 ? null : data.GetAlphamaps(0, 0, resolution, resolution);
            Vector3 corner = terrain.transform.position;
            Vector3 size = data.size;
            float reach = Mathf.Max(size.x / resolution, 1f);

            bool slopes = false;
            bool hollows = false;
            foreach (Rule rule in rules)
            {
                slopes |= rule.measure == Measure.Slope;
                hollows |= rule.measure == Measure.Hollow;
            }

            var maps = new float[resolution, resolution, layers];
            var weights = new float[layers];
            var totals = new double[layers];
            double covered = 0d;

            for (int row = 0; row < resolution; row++)
            for (int column = 0; column < resolution; column++)
            {
                float taken = before == null ? 1f : before[row, column, marked];
                if (taken <= 0f)
                {
                    for (int layer = 0; layer < layers; layer++) maps[row, column, layer] = before[row, column, layer];
                    continue;
                }

                covered += taken;

                float x = corner.x + (column + 0.5f) / resolution * size.x;
                float z = corner.z + (row + 0.5f) / resolution * size.z;
                float height = ground.Height(x, z);
                float slope = slopes ? Slope(ground, x, z, reach) : 0f;
                float hollow = hollows ? Hollow(ground, x, z, height) : 0f;

                Array.Clear(weights, 0, layers);
                weights[channels[0]] = 1f;

                for (int i = 0; i < rules.Count; i++)
                {
                    Rule rule = rules[i];
                    if (channels[i + 1] < 0 || rule.strength <= 0f) continue;

                    float value = rule.measure == Measure.Slope ? slope : rule.measure == Measure.Height ? height : hollow;
                    float share = rule.strength * Membership(value, rule.from, rule.to, rule.blend);
                    if (share <= 0f) continue;

                    for (int layer = 0; layer < layers; layer++) weights[layer] *= 1f - share;
                    weights[channels[i + 1]] += share;
                }

                for (int layer = 0; layer < layers; layer++)
                {
                    float kept = before == null || layer == marked ? 0f : before[row, column, layer];
                    maps[row, column, layer] = weights[layer] * taken + kept;
                    totals[layer] += weights[layer] * taken;
                }
            }

            data.SetAlphamaps(0, 0, maps);
            EditorUtility.SetDirty(data);

            for (int layer = 0; layer < layers; layer++)
            {
                if (palette[layer] == null || totals[layer] <= 0d) continue;

                shares.TryGetValue(palette[layer], out double sum);
                shares[palette[layer]] = sum + totals[layer];
            }

            return covered;
        }

        private bool Marked(TerrainData data)
        {
            int channel = Array.IndexOf(data.terrainLayers, marker);
            if (channel < 0) return false;

            int resolution = data.alphamapResolution;
            float[,,] maps = data.GetAlphamaps(0, 0, resolution, resolution);

            for (int row = 0; row < resolution; row++)
            for (int column = 0; column < resolution; column++)
                if (maps[row, column, channel] > 0f)
                    return true;

            return false;
        }

        // Where the background and every rule's layer sit in the terrain's palette; missing layers are appended, so painted maps keep their meaning
        private int[] Palette(TerrainData data)
        {
            var palette = new List<TerrainLayer>(data.terrainLayers);

            int Index(TerrainLayer layer)
            {
                if (layer == null) return -1;

                int found = palette.IndexOf(layer);
                if (found >= 0) return found;

                palette.Add(layer);
                return palette.Count - 1;
            }

            var channels = new int[rules.Count + 1];
            channels[0] = Index(background);
            for (int i = 0; i < rules.Count; i++) channels[i + 1] = Index(rules[i].layer);

            if (palette.Count != data.terrainLayers.Length) data.terrainLayers = palette.ToArray();

            return channels;
        }

        private static float Slope(Ground ground, float x, float z, float reach)
        {
            float acrossX = (ground.Height(x + reach, z) - ground.Height(x - reach, z)) / (2f * reach);
            float acrossZ = (ground.Height(x, z + reach) - ground.Height(x, z - reach)) / (2f * reach);

            return Mathf.Atan(Mathf.Sqrt(acrossX * acrossX + acrossZ * acrossZ)) * Mathf.Rad2Deg;
        }

        private float Hollow(Ground ground, float x, float z, float height)
        {
            float around = ground.Height(x + hollowRadius, z) + ground.Height(x - hollowRadius, z) +
                           ground.Height(x, z + hollowRadius) + ground.Height(x, z - hollowRadius);

            return around / 4f - height;
        }

        private static float Membership(float value, float from, float to, float blend)
        {
            if (blend <= 0f) return value >= from && value <= to ? 1f : 0f;

            return Ease((value - from + blend) / blend) * Ease((to + blend - value) / blend);
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
