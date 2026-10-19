using System;
using System.IO;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace Shooter.Editor
{
    public class IconStudioTool : EditorWindow
    {
        private const float CameraDistance = 10f;
        private const int LastLayer = 31;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [Serializable]
        private class IconSetup
        {
            public Vector3 sightAngles = new(18f, -35f, 0f);
            public float padding = 1.12f;
            public int size = 512;
            public Vector3 keyAngles = new(40f, -20f, 0f);
            public float keyLux = 20000f;
            public Vector3 fillAngles = new(10f, 150f, 0f);
            public float fillLux = 6000f;
            public float exposure = 11.4f;
        }

        [SerializeField] private IconSetup setup = new();
        private Vector2 scrollPosition;

        [MenuItem("Tools/Icon Studio")]
        private static void OpenWindow()
        {
            GetWindow<IconStudioTool>("Icon Studio");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Sight", EditorStyles.boldLabel);
            setup.sightAngles = EditorGUILayout.Vector3Field("Camera angles", setup.sightAngles);
            setup.padding = EditorGUILayout.Slider("Padding", setup.padding, 1f, 2f);
            setup.size = EditorGUILayout.IntPopup("Size", setup.size, new[] { "256", "512", "1024" }, new[] { 256, 512, 1024 });

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Light", EditorStyles.boldLabel);
            setup.keyAngles = EditorGUILayout.Vector3Field("Key angles", setup.keyAngles);
            setup.keyLux = EditorGUILayout.FloatField("Key lux", setup.keyLux);
            setup.fillAngles = EditorGUILayout.Vector3Field("Fill angles", setup.fillAngles);
            setup.fillLux = EditorGUILayout.FloatField("Fill lux", setup.fillLux);
            setup.exposure = EditorGUILayout.Slider("Exposure", setup.exposure, 8f, 18f);

            EditorGUILayout.Space();
            if (GUILayout.Button("Reset"))
            {
                setup = new IconSetup();
            }

            GameObject[] selectedPrefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(selectedPrefabs.Length == 0 ? "Select prefabs in the project" : $"Selected: {selectedPrefabs.Length}");

            using (new EditorGUI.DisabledScope(selectedPrefabs.Length == 0))
            {
                if (GUILayout.Button("Render", GUILayout.Height(30f)))
                {
                    RenderIcons(selectedPrefabs);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void RenderIcons(GameObject[] prefabs)
        {
            int renderedIcons = 0;
            foreach (GameObject prefab in prefabs)
            {
                if (ShootIcon(prefab, setup) != null)
                {
                    renderedIcons++;
                }
            }

            Log.Info($"Icon studio rendered {renderedIcons} icons out of {prefabs.Length} selected prefabs");
        }

        private static string ShootIcon(GameObject prefab, IconSetup setup)
        {
            int studioLayer = FindUnnamedLayer();
            if (studioLayer < 0)
            {
                Log.Warn("Every layer has a name, the icon studio needs one unnamed layer to shoot in");
                return null;
            }

            Light[] sceneLights = Array.FindAll(Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude), light => light.enabled);
            GameObject studio = new("Icon Studio") { hideFlags = HideFlags.HideAndDontSave };
            VolumeProfile volumeProfile = AddStudioVolume(studio, studioLayer, setup.exposure);
            try
            {
                foreach (Light light in sceneLights)
                {
                    light.enabled = false;
                }

                GameObject model = Object.Instantiate(prefab, studio.transform);
                MoveToLayer(model, studioLayer);
                if (!TryGetRendererBounds(model, out Bounds bounds))
                {
                    Log.Warn($"Prefab {prefab.name} has no renderers, nothing to shoot");
                    return null;
                }

                AddStudioLight(studio, studioLayer, setup.keyAngles, setup.keyLux);
                AddStudioLight(studio, studioLayer, setup.fillAngles, setup.fillLux);
                Camera camera = AddStudioCamera(studio, studioLayer, bounds, setup);

                Texture2D shot = RenderTransparentShot(camera, camera.GetComponent<HDAdditionalCameraData>(), setup.size);
                string iconPath = SaveIconNextToPrefab(prefab, shot);
                Object.DestroyImmediate(shot);
                return iconPath;
            }
            finally
            {
                foreach (Light light in sceneLights)
                {
                    light.enabled = true;
                }

                Object.DestroyImmediate(studio);
                Object.DestroyImmediate(volumeProfile);
            }
        }

        private static int FindUnnamedLayer()
        {
            for (int layer = LastLayer; layer >= 0; layer--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(layer))) return layer;
            }

            return -1;
        }

        private static void MoveToLayer(GameObject model, int layer)
        {
            foreach (Transform part in model.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = layer;
            }
        }

        private static GameObject AddStudioPart(GameObject studio, int layer, string partName)
        {
            GameObject part = new(partName) { layer = layer };
            part.transform.SetParent(studio.transform);
            return part;
        }

        private static bool TryGetRendererBounds(GameObject model, out Bounds bounds)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            bounds = default;
            if (renderers.Length == 0) return false;

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return true;
        }

        private static Camera AddStudioCamera(GameObject studio, int layer, Bounds bounds, IconSetup setup)
        {
            GameObject cameraObject = AddStudioPart(studio, layer, "Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = GetOrthographicSizeToFit(bounds, setup);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = CameraDistance * 4f;
            camera.cullingMask = 1 << layer;
            camera.transform.rotation = Quaternion.Euler(setup.sightAngles);
            camera.transform.position = bounds.center - camera.transform.forward * CameraDistance;

            HDAdditionalCameraData hdCameraData = cameraObject.AddComponent<HDAdditionalCameraData>();
            hdCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hdCameraData.volumeLayerMask = 1 << layer;
            hdCameraData.probeLayerMask = 0;
            return camera;
        }

        private static float GetOrthographicSizeToFit(Bounds bounds, IconSetup setup)
        {
            Quaternion sight = Quaternion.Euler(setup.sightAngles);
            Vector3 extents = bounds.extents;
            float widestExtent = 0f;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 cornerOffset = new((corner & 1) == 0 ? -extents.x : extents.x, (corner & 2) == 0 ? -extents.y : extents.y, (corner & 4) == 0 ? -extents.z : extents.z);
                Vector3 cornerInSight = Quaternion.Inverse(sight) * cornerOffset;
                widestExtent = Mathf.Max(widestExtent, Mathf.Abs(cornerInSight.x), Mathf.Abs(cornerInSight.y));
            }

            return widestExtent * setup.padding;
        }

        private static void AddStudioLight(GameObject studio, int layer, Vector3 angles, float lux)
        {
            GameObject lightObject = AddStudioPart(studio, layer, "Light");
            lightObject.transform.rotation = Quaternion.Euler(angles);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.cullingMask = 1 << layer;
            HDAdditionalLightData hdLightData = lightObject.AddComponent<HDAdditionalLightData>();
            hdLightData.EnableShadows(false);
            light.lightUnit = LightUnit.Lux;
            light.intensity = lux;
        }

        private static VolumeProfile AddStudioVolume(GameObject studio, int layer, float exposure)
        {
            VolumeProfile volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volumeProfile.hideFlags = HideFlags.HideAndDontSave;

            Exposure fixedExposure = volumeProfile.Add<Exposure>();
            fixedExposure.mode.overrideState = true;
            fixedExposure.mode.value = ExposureMode.Fixed;
            fixedExposure.fixedExposure.overrideState = true;
            fixedExposure.fixedExposure.value = exposure;

            Tonemapping tonemapping = volumeProfile.Add<Tonemapping>();
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            Bloom bloom = volumeProfile.Add<Bloom>();
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0f;

            Volume volume = AddStudioPart(studio, layer, "Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.profile = volumeProfile;
            return volumeProfile;
        }

        private static Texture2D RenderTransparentShot(Camera camera, HDAdditionalCameraData hdCameraData, int size)
        {
            Texture2D shotOnBlack = RenderOnBackground(camera, hdCameraData, Color.black, size);
            Texture2D shotOnWhite = RenderOnBackground(camera, hdCameraData, Color.white, size);

            Texture2D uncroppedShot = ExtractAlphaFromBackgrounds(shotOnBlack, shotOnWhite);
            Texture2D shot = CropToVisiblePixels(uncroppedShot);
            if (shot != uncroppedShot)
            {
                Object.DestroyImmediate(uncroppedShot);
            }

            Object.DestroyImmediate(shotOnBlack);
            Object.DestroyImmediate(shotOnWhite);
            return shot;
        }

        private static Texture2D RenderOnBackground(Camera camera, HDAdditionalCameraData hdCameraData, Color background, int size)
        {
            RenderTexture renderTarget = new(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previousActive = RenderTexture.active;

            hdCameraData.backgroundColorHDR = background;
            camera.targetTexture = renderTarget;
            camera.Render();

            RenderTexture.active = renderTarget;
            Texture2D shot = new(size, size, TextureFormat.RGBA32, false);
            shot.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
            shot.Apply();

            RenderTexture.active = previousActive;
            camera.targetTexture = null;
            renderTarget.Release();
            Object.DestroyImmediate(renderTarget);
            return shot;
        }

        private static Texture2D ExtractAlphaFromBackgrounds(Texture2D shotOnBlack, Texture2D shotOnWhite)
        {
            Color[] pixelsOnBlack = shotOnBlack.GetPixels();
            Color[] pixelsOnWhite = shotOnWhite.GetPixels();
            Color[] pixels = new Color[pixelsOnBlack.Length];

            float backgroundSpan = Mathf.Max(pixelsOnWhite[0].r - pixelsOnBlack[0].r, pixelsOnWhite[0].g - pixelsOnBlack[0].g, pixelsOnWhite[0].b - pixelsOnBlack[0].b);
            if (backgroundSpan <= 0.01f)
            {
                Log.Error("Both backgrounds came out the same, the icon stays opaque");
                backgroundSpan = 1f;
            }

            for (int index = 0; index < pixelsOnBlack.Length; index++)
            {
                Color onBlack = pixelsOnBlack[index];
                float liftOnWhite = (pixelsOnWhite[index].r - onBlack.r + pixelsOnWhite[index].g - onBlack.g + pixelsOnWhite[index].b - onBlack.b) / 3f;
                float alpha = Mathf.Clamp01(1f - liftOnWhite / backgroundSpan);
                pixels[index] = alpha <= 0.004f ? Color.clear : new Color(onBlack.r / alpha, onBlack.g / alpha, onBlack.b / alpha, alpha);
            }

            Texture2D shot = new(shotOnBlack.width, shotOnBlack.height, TextureFormat.RGBA32, false);
            shot.SetPixels(pixels);
            shot.Apply();
            return shot;
        }

        private static Texture2D CropToVisiblePixels(Texture2D shot)
        {
            Color[] pixels = shot.GetPixels();
            int left = shot.width;
            int right = -1;
            int bottom = shot.height;
            int top = -1;
            for (int y = 0; y < shot.height; y++)
            {
                for (int x = 0; x < shot.width; x++)
                {
                    if (pixels[y * shot.width + x].a <= 0.02f) continue;

                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                    bottom = Mathf.Min(bottom, y);
                    top = Mathf.Max(top, y);
                }
            }

            if (right < 0)
            {
                Log.Warn("The shot came out fully transparent, nothing to crop");
                return shot;
            }

            int width = right - left + 1;
            int height = top - bottom + 1;
            Texture2D cropped = new(width, height, TextureFormat.RGBA32, false);
            cropped.SetPixels(shot.GetPixels(left, bottom, width, height));
            cropped.Apply();
            return cropped;
        }

        private static string SaveIconNextToPrefab(GameObject prefab, Texture2D shot)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(prefab));
            string iconPath = Path.Combine(folder, prefab.name + "Icon.png").Replace('\\', '/');

            File.WriteAllBytes(iconPath, shot.EncodeToPNG());
            AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return iconPath;
        }
    }
}
