using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class MenuSetup
{
    private const string PanelSettingsPath = "Assets/UI/MenuPanelSettings.asset";
    private const string UxmlPath = "Assets/UI/Menu.uxml";
    private const string ThemePath = "Assets/UI/MenuTheme.tss";

    [MenuItem("Shooter/Setup Menu Scene")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Menu")
        {
            EditorUtility.DisplayDialog("Menu Setup", "Open the Menu scene first (Assets/Scenes/Menu).", "OK");
            return;
        }

        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (panelSettings == null)
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
        }

        var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
        if (theme != null)
            panelSettings.themeStyleSheet = theme;
        panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panelSettings.referenceResolution = new Vector2Int(1920, 1080);
        EditorUtility.SetDirty(panelSettings);

        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        if (uxml == null)
        {
            EditorUtility.DisplayDialog("Menu Setup", "Menu.uxml not found or not imported yet.", "OK");
            return;
        }

        GameObject menuUi = GameObject.Find("MenuUI");
        if (menuUi == null)
            menuUi = new GameObject("MenuUI");

        var document = menuUi.GetComponent<UIDocument>();
        if (document == null)
            document = menuUi.AddComponent<UIDocument>();
        document.panelSettings = panelSettings;
        document.visualTreeAsset = uxml;

        if (menuUi.GetComponent<MenuController>() == null)
            menuUi.AddComponent<MenuController>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("menu setup: done");
    }
}
