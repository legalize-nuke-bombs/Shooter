using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public class IconStudioWindow : EditorWindow
    {
        private IconSetup setup = IconStudio.Default();
        private Vector2 scroll;

        [MenuItem("Tools/Icon Studio")]
        private static void Open()
        {
            GetWindow<IconStudioWindow>("Icon Studio").Show();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Sight", EditorStyles.boldLabel);
            setup.SightAngles = EditorGUILayout.Vector3Field("Camera angles", setup.SightAngles);
            setup.Padding = EditorGUILayout.Slider("Padding", setup.Padding, 1f, 2f);
            setup.Size = EditorGUILayout.IntPopup("Size", setup.Size, new[] { "256", "512", "1024" }, new[] { 256, 512, 1024 });

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Light", EditorStyles.boldLabel);
            setup.KeyAngles = EditorGUILayout.Vector3Field("Key angles", setup.KeyAngles);
            setup.KeyIntensity = EditorGUILayout.FloatField("Key lux", setup.KeyIntensity);
            setup.FillAngles = EditorGUILayout.Vector3Field("Fill angles", setup.FillAngles);
            setup.FillIntensity = EditorGUILayout.FloatField("Fill lux", setup.FillIntensity);
            setup.Stop = EditorGUILayout.Slider("Exposure", setup.Stop, 8f, 18f);

            EditorGUILayout.Space();

            if (GUILayout.Button("Reset")) setup = IconStudio.Default();

            GameObject[] models = Selection.GetFiltered<GameObject>(SelectionMode.Assets);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(models.Length == 0
                ? "Select prefabs in the project"
                : $"Selected: {models.Length}");

            using (new EditorGUI.DisabledScope(models.Length == 0))
            {
                if (GUILayout.Button("Render", GUILayout.Height(30f))) Render(models);
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void Render(GameObject[] models)
        {
            int drawn = 0;

            foreach (GameObject model in models)
            {
                if (IconStudio.Shoot(model, setup) != null) drawn++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Icon studio rendered {drawn} icons out of {models.Length} selected prefabs");
        }
    }
}
