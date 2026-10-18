using System;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Shooter.Editor
{
    [InitializeOnLoad]
    public static class PlayFromFirstBuildScene
    {
        static PlayFromFirstBuildScene()
        {
            EditorBuildSettingsScene first = Array.Find(EditorBuildSettings.scenes, scene => scene.enabled);
            EditorSceneManager.playModeStartScene = first == null ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(first.path);
        }
    }
}
