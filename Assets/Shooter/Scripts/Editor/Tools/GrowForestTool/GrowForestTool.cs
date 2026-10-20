using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public class GrowForestTool : EditorWindow
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private ForestPlantingSettings settings = new();
        private SerializedObject serializedWindow;
        private string lastRunSummary;

        [MenuItem("Tools/Grow Forest")]
        private static void OpenWindow()
        {
            GetWindow<GrowForestTool>("Grow Forest");
        }

        private void OnEnable()
        {
            serializedWindow = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serializedWindow.Update();
            DrawSettingsField(nameof(ForestPlantingSettings.species), "Species", true);
            DrawSettingsField(nameof(ForestPlantingSettings.layerDensities), "Density on ground layers", true);
            DrawSettingsField(nameof(ForestPlantingSettings.spacing), "Spacing between trunks, m");
            DrawSettingsField(nameof(ForestPlantingSettings.groveSize), "Grove size, m");
            DrawSettingsField(nameof(ForestPlantingSettings.patchiness), "Patchiness");
            DrawSettingsField(nameof(ForestPlantingSettings.steepestSlope), "Steepest slope, degrees");
            DrawSettingsField(nameof(ForestPlantingSettings.treelineHeight), "Treeline height, m");
            DrawSettingsField(nameof(ForestPlantingSettings.distanceFromWater), "Distance from water, m");
            DrawSettingsField(nameof(ForestPlantingSettings.clearanceAroundObjects), "Clearance around objects, m");
            DrawSettingsField(nameof(ForestPlantingSettings.obstacles), "Obstacle layers");
            serializedWindow.ApplyModifiedProperties();

            EditorGUILayout.LabelField($"Forest is full on layers missing from the list and thins out over {ForestPlantingRun.TreelineFade} m below the treeline. " +
                                       "Share is a species' part of the forest, scale range is how big its trees grow. Growing replaces the trees of these species " +
                                       "on the chosen terrains, rocks and bushes stay, and trees keep clear of them and of anything on the obstacle layers", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            var selectedTerrains = new List<Terrain>(Selection.GetFiltered<Terrain>(SelectionMode.Unfiltered));
            EditorGUILayout.LabelField(selectedTerrains.Count == 0 ? "Select the terrains to grow forest on" : $"{selectedTerrains.Count} selected terrains");

            using (new EditorGUI.DisabledScope(selectedTerrains.Count == 0))
            {
                if (GUILayout.Button("Grow forest"))
                {
                    GrowForestOn(selectedTerrains);
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

        private void DrawSettingsField(string propertyName, string label, bool includeChildren = false)
        {
            EditorGUILayout.PropertyField(serializedWindow.FindProperty(nameof(settings)).FindPropertyRelative(propertyName), new GUIContent(label), includeChildren);
        }

        private void GrowForestOn(List<Terrain> terrains)
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

            lastRunSummary = new ForestPlantingRun(settings, new List<Terrain>(Terrain.activeTerrains)).PlantForestOn(terrains);
            Log.Info(lastRunSummary);
        }
    }
}
