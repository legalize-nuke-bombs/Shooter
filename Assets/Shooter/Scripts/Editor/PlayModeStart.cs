using UnityEditor;
using UnityEditor.SceneManagement;

namespace Shooter.Editor
{
    [InitializeOnLoad]
    public static class PlayModeStart
    {
        private const string BootScene = "Assets/Shooter/Data/Scenes/Boot.unity";

        static PlayModeStart()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScene);
        }
    }
}
