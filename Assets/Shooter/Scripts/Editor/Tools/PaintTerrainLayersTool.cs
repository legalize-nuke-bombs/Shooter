using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public class PaintTerrainLayersTool : EditorWindow
    {
        private const string UndoName = "Paint terrain layers";
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

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

        private sealed class HeightsAcrossTerrains
        {
            private readonly List<Tile> tiles = new();
            private Tile last;

            public HeightsAcrossTerrains(IEnumerable<Terrain> terrains)
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

            public float At(float x, float z)
            {
                if (last == null || !Inside(last, x, z))
                {
                    Tile found = null;
                    foreach (Tile tile in tiles)
                    {
                        if (Inside(tile, x, z))
                        {
                            found = tile;
                            break;
                        }
                    }

                    last = found ?? last ?? tiles[0];
                }

                return Sample(last, x, z);
            }

            private static bool Inside(Tile tile, float x, float z)
            {
                return x >= tile.Corner.x && x <= tile.Corner.x + tile.Size.x && z >= tile.Corner.z && z <= tile.Corner.z + tile.Size.z;
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

                float[,] heights = tile.Heights;
                float near = heights[row, column] + (heights[row, column + 1] - heights[row, column]) * across;
                float far = heights[row + 1, column] + (heights[row + 1, column + 1] - heights[row + 1, column]) * across;

                return (near + (far - near) * along) * tile.Size.y + tile.Corner.y;
            }
        }

        [SerializeField] private TerrainLayer background;
        [SerializeField] private float hollowRadius = 12f;
        [SerializeField] private List<Rule> rules = new();
        [SerializeField] private float softness = 2f;
        [SerializeField] private TerrainLayer marker;

        private string result;
        private SerializedObject serialized;

        [MenuItem("Tools/Paint Terrain Layers")]
        private static void Open()
        {
            GetWindow<PaintTerrainLayersTool>("Paint Terrain Layers");
        }

        private void OnEnable()
        {
            serialized = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(background)), new GUIContent("Background"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(hollowRadius)), new GUIContent("Hollow radius, m"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(rules)), new GUIContent("Rules, later over earlier"), true);
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(softness)), new GUIContent("Edge softness, cells"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(marker)), new GUIContent("Only where painted with"));
            serialized.ApplyModifiedProperties();

            EditorGUILayout.LabelField("Slope in degrees, height in world metres, hollow in metres below the ground around. Between a ledge and a sheer wall " +
                                       "the slope jumps within one map cell and the border comes out as stairs: softness blurs it over that many cells, " +
                                       "which is the band height blend needs; 0 paints cell to cell. With a marker layer set, only the ground painted " +
                                       "with it is repainted, the rest stays as it is", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            var targets = new List<Terrain>(Selection.GetFiltered<Terrain>(SelectionMode.Unfiltered));
            EditorGUILayout.LabelField(targets.Count == 0 ? "Select the terrains to paint" : $"{targets.Count} selected terrains");

            using (new EditorGUI.DisabledScope(targets.Count == 0))
            {
                if (GUILayout.Button(marker == null ? "Paint everything" : $"Paint over {marker.name}"))
                {
                    Paint(targets);
                }
            }

            if (!string.IsNullOrEmpty(result))
            {
                EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
            }
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void Paint(List<Terrain> targets)
        {
            if (background == null)
            {
                Log.Warn("No background layer is set, nothing to paint");
                return;
            }

            DateTime started = DateTime.Now;
            HeightsAcrossTerrains heights = new(Terrain.activeTerrains);
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
                    double covered = Paint(targets[i], heights, shares);
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
                result = $"Nothing painted: no selected terrain is painted with {marker.name}";
                Log.Warn(result);
                return;
            }

            StringBuilder summary = new($"Painted {painted} terrains in {(DateTime.Now - started).TotalSeconds:F1} s:");
            foreach (KeyValuePair<TerrainLayer, double> share in shares)
            {
                summary.Append($" {share.Key.name} {share.Value / texels:P0}");
            }

            result = summary.ToString();
            Log.Info(result);
        }

        private double Paint(Terrain terrain, HeightsAcrossTerrains heights, Dictionary<TerrainLayer, double> shares)
        {
            TerrainData data = terrain.terrainData;
            if (marker != null && !Marked(data)) return 0d;

            Undo.RegisterCompleteObjectUndo(data, UndoName);
            Undo.RegisterCompleteObjectUndo(data.alphamapTextures, UndoName);

            int[] channels = ChannelsAppendingMissingLayers(data);
            TerrainLayer[] palette = data.terrainLayers;
            int layers = palette.Length;
            int resolution = data.alphamapResolution;
            int markerChannel = marker == null ? -1 : Array.IndexOf(palette, marker);
            float[,,] before = markerChannel < 0 ? null : data.GetAlphamaps(0, 0, resolution, resolution);
            Vector3 corner = terrain.transform.position;
            Vector3 size = data.size;
            float reach = Mathf.Max(size.x / resolution, 1f);

            int blurMargin = Mathf.CeilToInt(Mathf.Max(softness, 0f));
            float[,,] ruled = WeightsByRules(heights, corner, size, resolution, reach, blurMargin, channels, layers);
            if (blurMargin > 0)
            {
                ruled = GaussianBlurred(ruled, softness, blurMargin);
            }

            float[,,] maps = new float[resolution, resolution, layers];
            double[] totals = new double[layers];
            double covered = 0d;

            for (int row = 0; row < resolution; row++)
            {
                for (int column = 0; column < resolution; column++)
                {
                    float markerShare = before == null ? 1f : before[row, column, markerChannel];
                    if (markerShare <= 0f)
                    {
                        for (int layer = 0; layer < layers; layer++)
                        {
                            maps[row, column, layer] = before[row, column, layer];
                        }

                        continue;
                    }

                    covered += markerShare;

                    for (int layer = 0; layer < layers; layer++)
                    {
                        float weight = ruled[row + blurMargin, column + blurMargin, layer];
                        float kept = before == null || layer == markerChannel ? 0f : before[row, column, layer];
                        maps[row, column, layer] = weight * markerShare + kept;
                        totals[layer] += weight * markerShare;
                    }
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

        private float[,,] WeightsByRules(HeightsAcrossTerrains heights, Vector3 corner, Vector3 size, int resolution, float reach, int blurMargin, int[] channels, int layers)
        {
            bool slopes = false;
            bool hollows = false;
            foreach (Rule rule in rules)
            {
                slopes |= rule.measure == Measure.Slope;
                hollows |= rule.measure == Measure.Hollow;
            }

            int span = resolution + blurMargin * 2;
            float[,,] ruled = new float[span, span, layers];
            float[] weights = new float[layers];

            for (int row = 0; row < span; row++)
            {
                for (int column = 0; column < span; column++)
                {
                    float x = corner.x + (column - blurMargin + 0.5f) / resolution * size.x;
                    float z = corner.z + (row - blurMargin + 0.5f) / resolution * size.z;
                    float height = heights.At(x, z);
                    float slope = slopes ? Slope(heights, x, z, reach) : 0f;
                    float hollow = hollows ? Hollow(heights, x, z, height) : 0f;

                    Array.Clear(weights, 0, layers);
                    weights[channels[0]] = 1f;

                    for (int i = 0; i < rules.Count; i++)
                    {
                        Rule rule = rules[i];
                        if (channels[i + 1] < 0 || rule.strength <= 0f) continue;

                        float value = rule.measure == Measure.Slope ? slope : rule.measure == Measure.Height ? height : hollow;
                        float share = rule.strength * Membership(value, rule.from, rule.to, rule.blend);
                        if (share <= 0f) continue;

                        for (int layer = 0; layer < layers; layer++)
                        {
                            weights[layer] *= 1f - share;
                        }

                        weights[channels[i + 1]] += share;
                    }

                    for (int layer = 0; layer < layers; layer++)
                    {
                        ruled[row, column, layer] = weights[layer];
                    }
                }
            }

            return ruled;
        }

        private static float[,,] GaussianBlurred(float[,,] ruled, float softness, int blurMargin)
        {
            float[] kernel = new float[blurMargin * 2 + 1];
            float sigma = softness / 2f;
            float sum = 0f;
            for (int i = 0; i < kernel.Length; i++)
            {
                float offset = i - blurMargin;
                kernel[i] = Mathf.Exp(-offset * offset / (2f * sigma * sigma));
                sum += kernel[i];
            }

            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] /= sum;
            }

            int span = ruled.GetLength(0);
            int layers = ruled.GetLength(2);
            float[,,] across = new float[span, span, layers];
            float[,,] along = new float[span, span, layers];

            for (int row = 0; row < span; row++)
            {
                for (int column = 0; column < span; column++)
                {
                    for (int layer = 0; layer < layers; layer++)
                    {
                        float blurred = 0f;
                        for (int i = 0; i < kernel.Length; i++)
                        {
                            blurred += ruled[row, Mathf.Clamp(column + i - blurMargin, 0, span - 1), layer] * kernel[i];
                        }

                        across[row, column, layer] = blurred;
                    }
                }
            }

            for (int row = 0; row < span; row++)
            {
                for (int column = 0; column < span; column++)
                {
                    for (int layer = 0; layer < layers; layer++)
                    {
                        float blurred = 0f;
                        for (int i = 0; i < kernel.Length; i++)
                        {
                            blurred += across[Mathf.Clamp(row + i - blurMargin, 0, span - 1), column, layer] * kernel[i];
                        }

                        along[row, column, layer] = blurred;
                    }
                }
            }

            return along;
        }

        private bool Marked(TerrainData data)
        {
            int channel = Array.IndexOf(data.terrainLayers, marker);
            if (channel < 0) return false;

            int resolution = data.alphamapResolution;
            float[,,] maps = data.GetAlphamaps(0, 0, resolution, resolution);

            for (int row = 0; row < resolution; row++)
            {
                for (int column = 0; column < resolution; column++)
                {
                    if (maps[row, column, channel] > 0f) return true;
                }
            }

            return false;
        }

        private int[] ChannelsAppendingMissingLayers(TerrainData data)
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

            int[] channels = new int[rules.Count + 1];
            channels[0] = Index(background);
            for (int i = 0; i < rules.Count; i++)
            {
                channels[i + 1] = Index(rules[i].layer);
            }

            if (palette.Count != data.terrainLayers.Length)
            {
                data.terrainLayers = palette.ToArray();
            }

            return channels;
        }

        private static float Slope(HeightsAcrossTerrains heights, float x, float z, float reach)
        {
            float acrossX = (heights.At(x + reach, z) - heights.At(x - reach, z)) / (2f * reach);
            float acrossZ = (heights.At(x, z + reach) - heights.At(x, z - reach)) / (2f * reach);

            return Mathf.Atan(Mathf.Sqrt(acrossX * acrossX + acrossZ * acrossZ)) * Mathf.Rad2Deg;
        }

        private float Hollow(HeightsAcrossTerrains heights, float x, float z, float height)
        {
            float around = heights.At(x + hollowRadius, z) + heights.At(x - hollowRadius, z) + heights.At(x, z + hollowRadius) + heights.At(x, z - hollowRadius);

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
