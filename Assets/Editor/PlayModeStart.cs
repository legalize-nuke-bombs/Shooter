using UnityEditor;
using UnityEditor.SceneManagement;

namespace Shooter.Editing
{
    [InitializeOnLoad]
    public static class PlayModeStart
    {
        private const string BootScene = "Assets/Scenes/Boot.unity";

        static PlayModeStart()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScene);
        }
    }
}
