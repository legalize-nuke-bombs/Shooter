using System.Diagnostics;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Shooter.Editor
{
    public class BakeGlobalIlluminationTool : EditorWindow
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly string[] FailureMarkers = { "Light baking failed", "PostProcessingOutput\\APVBake" };

        private static Stopwatch stopwatch;
        private static Scene bakedScene;
        private static bool sceneWasClean;
        private static bool failed;

        [MenuItem("Tools/Shooter/World/Bake GI")]
        private static void OpenWindow()
        {
            GetWindow<BakeGlobalIlluminationTool>("Bake GI");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Bakes the sky into the probe volumes the buildings carry. Lamps are live, so the lightmapper is not used: " +
                                       "only how much sky each probe sees, and which probes are stuck in walls or the ground", EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUI.DisabledScope(AdaptiveProbeVolumes.isRunning || EditorApplication.isPlaying))
            {
                if (GUILayout.Button(AdaptiveProbeVolumes.isRunning ? "Baking..." : "Bake"))
                {
                    Bake();
                }
            }
        }

        private static void Bake()
        {
            ProbeVolumeBakingSet bakingSet = ProbeReferenceVolume.instance.currentBakingSet;
            if (bakingSet == null)
            {
                Log.Warn("The open scene has no probe volume baking set, nothing to bake");
                return;
            }

            // Unity runs virtual offset from inside its lightmapper step, which ours replaces
            var serializedSet = new SerializedObject(bakingSet);
            serializedSet.FindProperty("settings.virtualOffsetSettings.useVirtualOffset").boolValue = false;
            serializedSet.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(bakingSet);

            bakedScene = SceneManager.GetActiveScene();
            sceneWasClean = !bakedScene.isDirty;
            failed = false;
            stopwatch = Stopwatch.StartNew();

            AdaptiveProbeVolumes.SetLightingBakerOverride(new SkyOnlyLightingBaker());
            AdaptiveProbeVolumes.SetSkyOcclusionBakerOverride(null);
            Application.logMessageReceived += WatchForFailure;
            if (!AdaptiveProbeVolumes.BakeAsync())
            {
                Finish();
                Log.Warn("Probe volume bake did not start");
                return;
            }

            EditorApplication.update += WaitForBake;
            Log.Info($"Baking GI with {bakingSet.minDistanceBetweenProbes} m between probes");
        }

        private static void WaitForBake()
        {
            if (AdaptiveProbeVolumes.isRunning) return;

            EditorApplication.update -= WaitForBake;
            Finish();
            if (failed)
            {
                Log.Error($"GI bake failed after {stopwatch.Elapsed.TotalSeconds:F0} s");
                return;
            }

            if (sceneWasClean && bakedScene.isLoaded) EditorSceneManager.SaveScene(bakedScene);
            Log.Info($"GI baked in {stopwatch.Elapsed.TotalSeconds:F0} s");
        }

        private static void Finish()
        {
            Application.logMessageReceived -= WatchForFailure;
            AdaptiveProbeVolumes.SetLightingBakerOverride(null);
        }

        // After a failure the package keeps reading a result file that never came, every editor frame, until the bake is cancelled
        private static void WatchForFailure(string message, string stackTrace, LogType type)
        {
            if (failed || type == LogType.Log || type == LogType.Warning) return;

            foreach (string marker in FailureMarkers)
            {
                if (!message.Contains(marker) && !stackTrace.Contains(marker)) continue;

                failed = true;
                AdaptiveProbeVolumes.Cancel();
                return;
            }
        }
    }
}
