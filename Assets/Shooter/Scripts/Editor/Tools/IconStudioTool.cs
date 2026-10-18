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
        private const float Distance = 10f;
        private const int LastLayer = 31;
        private static readonly Journal Log = Logs.Here();

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
        private Vector2 scroll;

        [MenuItem("Tools/Icon Studio")]
        private static void Open()
        {
            GetWindow<IconStudioTool>("Icon Studio");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

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

            GameObject[] prefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(prefabs.Length == 0 ? "Select prefabs in the project" : $"Selected: {prefabs.Length}");

            using (new EditorGUI.DisabledScope(prefabs.Length == 0))
            {
                if (GUILayout.Button("Render", GUILayout.Height(30f)))
                {
                    Render(prefabs);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void Render(GameObject[] prefabs)
        {
            int rendered = 0;
            foreach (GameObject prefab in prefabs)
            {
                if (Shoot(prefab, setup) != null)
                {
                    rendered++;
                }
            }

            Log.Info($"Icon studio rendered {rendered} icons out of {prefabs.Length} selected prefabs");
        }

        private static string Shoot(GameObject prefab, IconSetup setup)
        {
            int layer = FreeLayer();
            if (layer < 0)
            {
                Log.Warn("Every layer has a name, the icon studio needs one unnamed layer to shoot in");
                return null;
            }

            Light[] sceneLights = Array.FindAll(Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude), light => light.enabled);
            var studio = new GameObject("Icon Studio") { hideFlags = HideFlags.HideAndDontSave };
            VolumeProfile profile = StudioVolume(studio, layer, setup.exposure);
            try
            {
                foreach (Light light in sceneLights)
                {
                    light.enabled = false;
                }

                GameObject model = Object.Instantiate(prefab, studio.transform);
                MoveToLayer(model, layer);
                if (!RendererBounds(model, out Bounds bounds))
                {
                    Log.Warn($"Prefab {prefab.name} has no renderers, nothing to shoot");
                    return null;
                }

                StudioLight(studio, layer, setup.keyAngles, setup.keyLux);
                StudioLight(studio, layer, setup.fillAngles, setup.fillLux);
                Camera camera = StudioCamera(studio, layer, bounds, setup);

                Texture2D shot = TransparentShot(camera, camera.GetComponent<HDAdditionalCameraData>(), setup.size);
                string path = Save(prefab, shot);
                Object.DestroyImmediate(shot);
                return path;
            }
            finally
            {
                foreach (Light light in sceneLights)
                {
                    light.enabled = true;
                }

                Object.DestroyImmediate(studio);
                Object.DestroyImmediate(profile);
            }
        }

        private static int FreeLayer()
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

        private static GameObject StudioPart(GameObject studio, int layer, string name)
        {
            var part = new GameObject(name) { layer = layer };
            part.transform.SetParent(studio.transform);
            return part;
        }

        private static bool RendererBounds(GameObject model, out Bounds bounds)
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

        private static Camera StudioCamera(GameObject studio, int layer, Bounds bounds, IconSetup setup)
        {
            GameObject holder = StudioPart(studio, layer, "Camera");
            Camera camera = holder.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = OrthographicSize(bounds, setup);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = Distance * 4f;
            camera.cullingMask = 1 << layer;
            camera.transform.rotation = Quaternion.Euler(setup.sightAngles);
            camera.transform.position = bounds.center - camera.transform.forward * Distance;

            HDAdditionalCameraData data = holder.AddComponent<HDAdditionalCameraData>();
            data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            data.volumeLayerMask = 1 << layer;
            data.probeLayerMask = 0;
            return camera;
        }

        private static float OrthographicSize(Bounds bounds, IconSetup setup)
        {
            var sight = Quaternion.Euler(setup.sightAngles);
            Vector3 extents = bounds.extents;
            float widest = 0f;
            for (int corner = 0; corner < 8; corner++)
            {
                var offset = new Vector3(
                    (corner & 1) == 0 ? -extents.x : extents.x,
                    (corner & 2) == 0 ? -extents.y : extents.y,
                    (corner & 4) == 0 ? -extents.z : extents.z);
                Vector3 seen = Quaternion.Inverse(sight) * offset;
                widest = Mathf.Max(widest, Mathf.Abs(seen.x), Mathf.Abs(seen.y));
            }

            return widest * setup.padding;
        }

        private static void StudioLight(GameObject studio, int layer, Vector3 angles, float lux)
        {
            GameObject holder = StudioPart(studio, layer, "Light");
            holder.transform.rotation = Quaternion.Euler(angles);

            Light light = holder.AddComponent<Light>();
            light.type = LightType.Directional;
            light.cullingMask = 1 << layer;
            HDAdditionalLightData data = holder.AddComponent<HDAdditionalLightData>();
            data.EnableShadows(false);
            light.lightUnit = LightUnit.Lux;
            light.intensity = lux;
        }

        private static VolumeProfile StudioVolume(GameObject studio, int layer, float exposure)
        {
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;

            Exposure fixedExposure = profile.Add<Exposure>();
            fixedExposure.mode.overrideState = true;
            fixedExposure.mode.value = ExposureMode.Fixed;
            fixedExposure.fixedExposure.overrideState = true;
            fixedExposure.fixedExposure.value = exposure;

            Tonemapping tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            Bloom bloom = profile.Add<Bloom>();
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0f;

            Volume volume = StudioPart(studio, layer, "Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.profile = profile;
            return profile;
        }

        private static Texture2D TransparentShot(Camera camera, HDAdditionalCameraData data, int size)
        {
            Texture2D onBlack = RenderOn(camera, data, Color.black, size);
            Texture2D onWhite = RenderOn(camera, data, Color.white, size);

            Texture2D full = AlphaFromBackgrounds(onBlack, onWhite);
            Texture2D shot = Crop(full);
            if (shot != full)
            {
                Object.DestroyImmediate(full);
            }

            Object.DestroyImmediate(onBlack);
            Object.DestroyImmediate(onWhite);
            return shot;
        }

        private static Texture2D RenderOn(Camera camera, HDAdditionalCameraData data, Color background, int size)
        {
            var target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;

            data.backgroundColorHDR = background;
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            var shot = new Texture2D(size, size, TextureFormat.RGBA32, false);
            shot.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
            shot.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(target);
            return shot;
        }

        private static Texture2D AlphaFromBackgrounds(Texture2D onBlack, Texture2D onWhite)
        {
            Color[] dark = onBlack.GetPixels();
            Color[] light = onWhite.GetPixels();
            var pixels = new Color[dark.Length];

            float span = Mathf.Max(light[0].r - dark[0].r, light[0].g - dark[0].g, light[0].b - dark[0].b);
            if (span <= 0.01f)
            {
                Log.Error("Both backgrounds came out the same, the icon stays opaque");
                span = 1f;
            }

            for (int index = 0; index < dark.Length; index++)
            {
                Color over = dark[index];
                float lifted = (light[index].r - over.r + light[index].g - over.g + light[index].b - over.b) / 3f;
                float alpha = Mathf.Clamp01(1f - lifted / span);
                pixels[index] = alpha <= 0.004f ? Color.clear : new Color(over.r / alpha, over.g / alpha, over.b / alpha, alpha);
            }

            var shot = new Texture2D(onBlack.width, onBlack.height, TextureFormat.RGBA32, false);
            shot.SetPixels(pixels);
            shot.Apply();
            return shot;
        }

        private static Texture2D Crop(Texture2D shot)
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
            var cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);
            cropped.SetPixels(shot.GetPixels(left, bottom, width, height));
            cropped.Apply();
            return cropped;
        }

        private static string Save(GameObject prefab, Texture2D shot)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(prefab));
            string path = Path.Combine(folder, prefab.name + "Icon.png").Replace('\\', '/');

            File.WriteAllBytes(path, shot.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return path;
        }
    }
}
