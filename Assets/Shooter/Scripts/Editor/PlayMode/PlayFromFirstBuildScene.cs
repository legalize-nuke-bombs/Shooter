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
            EditorBuildSettingsScene firstEnabledScene = Array.Find(EditorBuildSettings.scenes, scene => scene.enabled);
            EditorSceneManager.playModeStartScene = firstEnabledScene == null ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(firstEnabledScene.path);
        }
    }
}
