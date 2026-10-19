using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public class GrowForestTool : EditorWindow
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private ForestPlantingSettings settings = new();
        private SerializedObject serialized;
        private string result;

        [MenuItem("Tools/Grow Forest")]
        private static void Open()
        {
            GetWindow<GrowForestTool>("Grow Forest");
        }

        private void OnEnable()
        {
            serialized = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serialized.Update();
            Field(nameof(ForestPlantingSettings.species), "Species", true);
            Field(nameof(ForestPlantingSettings.layerDensities), "Density on ground layers", true);
            Field(nameof(ForestPlantingSettings.spacing), "Spacing between trunks, m");
            Field(nameof(ForestPlantingSettings.groveSize), "Grove size, m");
            Field(nameof(ForestPlantingSettings.patchiness), "Patchiness");
            Field(nameof(ForestPlantingSettings.steepestSlope), "Steepest slope, degrees");
            Field(nameof(ForestPlantingSettings.treelineHeight), "Treeline height, m");
            Field(nameof(ForestPlantingSettings.distanceFromWater), "Distance from water, m");
            Field(nameof(ForestPlantingSettings.clearanceAroundObjects), "Clearance around objects, m");
            Field(nameof(ForestPlantingSettings.obstacles), "Obstacle layers");
            serialized.ApplyModifiedProperties();

            EditorGUILayout.LabelField($"Forest is full on layers missing from the list and thins out over {ForestPlantingRun.TreelineFade} m below the treeline. " +
                                       "Share is a species' part of the forest, scale range is how big its trees grow. Growing replaces the trees of these species " +
                                       "on the chosen terrains, rocks and bushes stay, and trees keep clear of them and of anything on the obstacle layers",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            var targets = new List<Terrain>(Selection.GetFiltered<Terrain>(SelectionMode.Unfiltered));
            EditorGUILayout.LabelField(targets.Count == 0 ? "Select the terrains to grow forest on" : $"{targets.Count} selected terrains");

            using (new EditorGUI.DisabledScope(targets.Count == 0))
            {
                if (GUILayout.Button("Grow forest"))
                {
                    Grow(targets);
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

        private void Field(string name, string label, bool children = false)
        {
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(settings)).FindPropertyRelative(name), new GUIContent(label), children);
        }

        private void Grow(List<Terrain> targets)
        {
            if (EditorApplication.isPlaying)
            {
                Log.Warn("Forest grows in edit mode only, stop the play mode first");
                return;
            }

            if (!settings.species.Exists(entry => entry.tree != null && entry.share > 0f))
            {
                Log.Warn("No species has both a tree and a share, nothing to grow");
                return;
            }

            result = new ForestPlantingRun(settings, new List<Terrain>(Terrain.activeTerrains)).Plant(targets);
            Log.Info(result);
        }
    }
}
