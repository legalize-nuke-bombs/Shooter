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

        private sealed class HeightsAcrossTerrains
        {
            private readonly List<Tile> tiles = new();
            private Tile lastSampledTile;

            public HeightsAcrossTerrains(IEnumerable<Terrain> terrains)
            {
                foreach (Terrain terrain in terrains)
                {
                    TerrainData terrainData = terrain.terrainData;
                    int resolution = terrainData.heightmapResolution;
                    tiles.Add(new Tile
                    {
                        Corner = terrain.transform.position,
                        Heights = terrainData.GetHeights(0, 0, resolution, resolution),
                        Resolution = resolution,
                        Size = terrainData.size
                    });
                }
            }

            public float GetHeightAt(float x, float z)
            {
                if (lastSampledTile == null || !IsInsideTile(lastSampledTile, x, z))
                {
                    Tile foundTile = null;
                    foreach (Tile tile in tiles)
                    {
                        if (IsInsideTile(tile, x, z))
                        {
                            foundTile = tile;
                            break;
                        }
                    }

                    lastSampledTile = foundTile ?? lastSampledTile ?? tiles[0];
                }

                return SampleHeight(lastSampledTile, x, z);
            }

            private static bool IsInsideTile(Tile tile, float x, float z)
            {
                return x >= tile.Corner.x && x <= tile.Corner.x + tile.Size.x && z >= tile.Corner.z && z <= tile.Corner.z + tile.Size.z;
            }

            private static float SampleHeight(Tile tile, float x, float z)
            {
                int lastIndex = tile.Resolution - 1;
                float u = Mathf.Clamp01((x - tile.Corner.x) / tile.Size.x) * lastIndex;
                float v = Mathf.Clamp01((z - tile.Corner.z) / tile.Size.z) * lastIndex;
                int column = Mathf.Min((int)u, lastIndex - 1);
                int row = Mathf.Min((int)v, lastIndex - 1);
                float across = u - column;
                float along = v - row;

                float[,] heights = tile.Heights;
                float nearHeight = heights[row, column] + (heights[row, column + 1] - heights[row, column]) * across;
                float farHeight = heights[row + 1, column] + (heights[row + 1, column + 1] - heights[row + 1, column]) * across;

                return (nearHeight + (farHeight - nearHeight) * along) * tile.Size.y + tile.Corner.y;
            }
        }

        [SerializeField] private TerrainLayer background;
        [SerializeField] private float hollowRadius = 12f;
        [SerializeField] private List<Rule> rules = new();
        [SerializeField] private float softness = 2f;
        [SerializeField] private TerrainLayer marker;

        private string lastRunSummary;
        private SerializedObject serializedWindow;

        [MenuItem("Tools/Paint Terrain Layers")]
        private static void OpenWindow()
        {
            GetWindow<PaintTerrainLayersTool>("Paint Terrain Layers");
        }

        private void OnEnable()
        {
            serializedWindow = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serializedWindow.Update();
            EditorGUILayout.PropertyField(serializedWindow.FindProperty(nameof(background)), new GUIContent("Background"));
            EditorGUILayout.PropertyField(serializedWindow.FindProperty(nameof(hollowRadius)), new GUIContent("Hollow radius, m"));
            EditorGUILayout.PropertyField(serializedWindow.FindProperty(nameof(rules)), new GUIContent("Rules, later over earlier"), true);
            EditorGUILayout.PropertyField(serializedWindow.FindProperty(nameof(softness)), new GUIContent("Edge softness, cells"));
            EditorGUILayout.PropertyField(serializedWindow.FindProperty(nameof(marker)), new GUIContent("Only where painted with"));
            serializedWindow.ApplyModifiedProperties();

            EditorGUILayout.LabelField("Slope in degrees, height in world metres, hollow in metres below the ground around. Between a ledge and a sheer wall " +
                                       "the slope jumps within one map cell and the border comes out as stairs: softness blurs it over that many cells, " +
                                       "which is the band height blend needs; 0 paints cell to cell. With a marker layer set, only the ground painted " +
                                       "with it is repainted, the rest stays as it is", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            var selectedTerrains = new List<Terrain>(Selection.GetFiltered<Terrain>(SelectionMode.Unfiltered));
            EditorGUILayout.LabelField(selectedTerrains.Count == 0 ? "Select the terrains to paint" : $"{selectedTerrains.Count} selected terrains");

            using (new EditorGUI.DisabledScope(selectedTerrains.Count == 0))
            {
                if (GUILayout.Button(marker == null ? "Paint everything" : $"Paint over {marker.name}"))
                {
                    PaintTerrains(selectedTerrains);
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

        private void PaintTerrains(List<Terrain> terrains)
        {
            if (background == null)
            {
                Log.Warn("No background layer is set, nothing to paint");
                return;
            }

            DateTime startTime = DateTime.Now;
            HeightsAcrossTerrains heightsAcrossTerrains = new(Terrain.activeTerrains);
            var coverageByLayer = new Dictionary<TerrainLayer, double>();
            double coveredTexels = 0d;
            int paintedTerrains = 0;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            try
            {
                for (int i = 0; i < terrains.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Paint Terrain Layers", terrains[i].name, (float)i / terrains.Count);
                    double coveredOnTerrain = PaintTerrain(terrains[i], heightsAcrossTerrains, coverageByLayer);
                    if (coveredOnTerrain <= 0d) continue;

                    coveredTexels += coveredOnTerrain;
                    paintedTerrains++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(undoGroup);
            }

            if (paintedTerrains == 0)
            {
                lastRunSummary = $"Nothing painted: no selected terrain is painted with {marker.name}";
                Log.Warn(lastRunSummary);
                return;
            }

            StringBuilder summary = new($"Painted {paintedTerrains} terrains in {(DateTime.Now - startTime).TotalSeconds:F1} s:");
            foreach (KeyValuePair<TerrainLayer, double> layerCoverage in coverageByLayer)
            {
                summary.Append($" {layerCoverage.Key.name} {layerCoverage.Value / coveredTexels:P0}");
            }

            lastRunSummary = summary.ToString();
            Log.Info(lastRunSummary);
        }

        private double PaintTerrain(Terrain terrain, HeightsAcrossTerrains heightsAcrossTerrains, Dictionary<TerrainLayer, double> coverageByLayer)
        {
            TerrainData terrainData = terrain.terrainData;
            if (marker != null && !IsPaintedWithMarker(terrainData)) return 0d;

            Undo.RegisterCompleteObjectUndo(terrainData, UndoName);
            Undo.RegisterCompleteObjectUndo(terrainData.alphamapTextures, UndoName);

            int[] channels = AppendMissingLayersAndGetChannels(terrainData);
            TerrainLayer[] terrainLayers = terrainData.terrainLayers;
            int layerCount = terrainLayers.Length;
            int resolution = terrainData.alphamapResolution;
            int markerChannel = marker == null ? -1 : Array.IndexOf(terrainLayers, marker);
            float[,,] alphamapsBefore = markerChannel < 0 ? null : terrainData.GetAlphamaps(0, 0, resolution, resolution);
            Vector3 terrainCorner = terrain.transform.position;
            Vector3 terrainSize = terrainData.size;
            float slopeReach = Mathf.Max(terrainSize.x / resolution, 1f);

            int blurMargin = Mathf.CeilToInt(Mathf.Max(softness, 0f));
            float[,,] ruledWeights = ComputeWeightsByRules(heightsAcrossTerrains, terrainCorner, terrainSize, resolution, slopeReach, blurMargin, channels, layerCount);
            if (blurMargin > 0)
            {
                ruledWeights = BlurWithGaussian(ruledWeights, softness, blurMargin);
            }

            float[,,] alphamaps = new float[resolution, resolution, layerCount];
            double[] totalWeights = new double[layerCount];
            double coveredTexels = 0d;

            for (int row = 0; row < resolution; row++)
            {
                for (int column = 0; column < resolution; column++)
                {
                    float markerShare = alphamapsBefore == null ? 1f : alphamapsBefore[row, column, markerChannel];
                    if (markerShare <= 0f)
                    {
                        for (int layer = 0; layer < layerCount; layer++)
                        {
                            alphamaps[row, column, layer] = alphamapsBefore[row, column, layer];
                        }

                        continue;
                    }

                    coveredTexels += markerShare;

                    for (int layer = 0; layer < layerCount; layer++)
                    {
                        float weight = ruledWeights[row + blurMargin, column + blurMargin, layer];
                        float keptWeight = alphamapsBefore == null || layer == markerChannel ? 0f : alphamapsBefore[row, column, layer];
                        alphamaps[row, column, layer] = weight * markerShare + keptWeight;
                        totalWeights[layer] += weight * markerShare;
                    }
                }
            }

            terrainData.SetAlphamaps(0, 0, alphamaps);
            EditorUtility.SetDirty(terrainData);

            for (int layer = 0; layer < layerCount; layer++)
            {
                if (terrainLayers[layer] == null || totalWeights[layer] <= 0d) continue;

                coverageByLayer.TryGetValue(terrainLayers[layer], out double sum);
                coverageByLayer[terrainLayers[layer]] = sum + totalWeights[layer];
            }

            return coveredTexels;
        }

        private float[,,] ComputeWeightsByRules(HeightsAcrossTerrains heightsAcrossTerrains, Vector3 terrainCorner, Vector3 terrainSize, int resolution, float slopeReach, int blurMargin, int[] channels, int layerCount)
        {
            bool needsSlope = false;
            bool needsHollow = false;
            foreach (Rule rule in rules)
            {
                needsSlope |= rule.measure == Measure.Slope;
                needsHollow |= rule.measure == Measure.Hollow;
            }

            int span = resolution + blurMargin * 2;
            float[,,] ruledWeights = new float[span, span, layerCount];
            float[] weights = new float[layerCount];

            for (int row = 0; row < span; row++)
            {
                for (int column = 0; column < span; column++)
                {
                    float x = terrainCorner.x + (column - blurMargin + 0.5f) / resolution * terrainSize.x;
                    float z = terrainCorner.z + (row - blurMargin + 0.5f) / resolution * terrainSize.z;
                    float height = heightsAcrossTerrains.GetHeightAt(x, z);
                    float slope = needsSlope ? MeasureSlope(heightsAcrossTerrains, x, z, slopeReach) : 0f;
                    float hollow = needsHollow ? MeasureHollow(heightsAcrossTerrains, x, z, height) : 0f;

                    Array.Clear(weights, 0, layerCount);
                    weights[channels[0]] = 1f;

                    for (int i = 0; i < rules.Count; i++)
                    {
                        Rule rule = rules[i];
                        if (channels[i + 1] < 0 || rule.strength <= 0f) continue;

                        float measuredValue = rule.measure == Measure.Slope ? slope : rule.measure == Measure.Height ? height : hollow;
                        float ruleShare = rule.strength * GetMembershipInRange(measuredValue, rule.from, rule.to, rule.blend);
                        if (ruleShare <= 0f) continue;

                        for (int layer = 0; layer < layerCount; layer++)
                        {
                            weights[layer] *= 1f - ruleShare;
                        }

                        weights[channels[i + 1]] += ruleShare;
                    }

                    for (int layer = 0; layer < layerCount; layer++)
                    {
                        ruledWeights[row, column, layer] = weights[layer];
                    }
                }
            }

            return ruledWeights;
        }

        private static float[,,] BlurWithGaussian(float[,,] ruledWeights, float softness, int blurMargin)
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

            int span = ruledWeights.GetLength(0);
            int layerCount = ruledWeights.GetLength(2);
            float[,,] blurredAcross = new float[span, span, layerCount];
            float[,,] blurredAlong = new float[span, span, layerCount];

            for (int row = 0; row < span; row++)
            {
                for (int column = 0; column < span; column++)
                {
                    for (int layer = 0; layer < layerCount; layer++)
                    {
                        float blurred = 0f;
                        for (int i = 0; i < kernel.Length; i++)
                        {
                            blurred += ruledWeights[row, Mathf.Clamp(column + i - blurMargin, 0, span - 1), layer] * kernel[i];
                        }

                        blurredAcross[row, column, layer] = blurred;
                    }
                }
            }

            for (int row = 0; row < span; row++)
            {
                for (int column = 0; column < span; column++)
                {
                    for (int layer = 0; layer < layerCount; layer++)
                    {
                        float blurred = 0f;
                        for (int i = 0; i < kernel.Length; i++)
                        {
                            blurred += blurredAcross[Mathf.Clamp(row + i - blurMargin, 0, span - 1), column, layer] * kernel[i];
                        }

                        blurredAlong[row, column, layer] = blurred;
                    }
                }
            }

            return blurredAlong;
        }

        private bool IsPaintedWithMarker(TerrainData terrainData)
        {
            int markerChannel = Array.IndexOf(terrainData.terrainLayers, marker);
            if (markerChannel < 0) return false;

            int resolution = terrainData.alphamapResolution;
            float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, resolution, resolution);

            for (int row = 0; row < resolution; row++)
            {
                for (int column = 0; column < resolution; column++)
                {
                    if (alphamaps[row, column, markerChannel] > 0f) return true;
                }
            }

            return false;
        }

        private int[] AppendMissingLayersAndGetChannels(TerrainData terrainData)
        {
            var terrainLayers = new List<TerrainLayer>(terrainData.terrainLayers);

            int GetChannelAppendingIfMissing(TerrainLayer layer)
            {
                if (layer == null) return -1;

                int foundChannel = terrainLayers.IndexOf(layer);
                if (foundChannel >= 0) return foundChannel;

                terrainLayers.Add(layer);
                return terrainLayers.Count - 1;
            }

            int[] channels = new int[rules.Count + 1];
            channels[0] = GetChannelAppendingIfMissing(background);
            for (int i = 0; i < rules.Count; i++)
            {
                channels[i + 1] = GetChannelAppendingIfMissing(rules[i].layer);
            }

            if (terrainLayers.Count != terrainData.terrainLayers.Length)
            {
                terrainData.terrainLayers = terrainLayers.ToArray();
            }

            return channels;
        }

        private static float MeasureSlope(HeightsAcrossTerrains heightsAcrossTerrains, float x, float z, float reach)
        {
            float gradientX = (heightsAcrossTerrains.GetHeightAt(x + reach, z) - heightsAcrossTerrains.GetHeightAt(x - reach, z)) / (2f * reach);
            float gradientZ = (heightsAcrossTerrains.GetHeightAt(x, z + reach) - heightsAcrossTerrains.GetHeightAt(x, z - reach)) / (2f * reach);

            return Mathf.Atan(Mathf.Sqrt(gradientX * gradientX + gradientZ * gradientZ)) * Mathf.Rad2Deg;
        }

        private float MeasureHollow(HeightsAcrossTerrains heightsAcrossTerrains, float x, float z, float height)
        {
            float heightSumAround = heightsAcrossTerrains.GetHeightAt(x + hollowRadius, z) + heightsAcrossTerrains.GetHeightAt(x - hollowRadius, z)
                                    + heightsAcrossTerrains.GetHeightAt(x, z + hollowRadius) + heightsAcrossTerrains.GetHeightAt(x, z - hollowRadius);

            return heightSumAround / 4f - height;
        }

        private static float GetMembershipInRange(float value, float from, float to, float blend)
        {
            if (blend <= 0f) return value >= from && value <= to ? 1f : 0f;

            return SmoothStep((value - from + blend) / blend) * SmoothStep((to + blend - value) / blend);
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
