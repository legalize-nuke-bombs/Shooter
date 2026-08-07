using System.Collections.Generic;
using System.IO;
using Shooter.Game.Loot;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Editor
{
    public static class ItemIconRenderer
    {
        private const string Menu = "Tools/Render Item Icons";
        private const int Layer = 11;
        private const int Size = 512;
        private const float Padding = 1.12f;
        private const float Distance = 10f;
        private const float SunIntensity = 20000f;
        private const float FillIntensity = 6000f;
        private const float Stop = 13.2f;

        private static readonly Vector3 SightAngles = new Vector3(18f, -35f, 0f);
        private static readonly Vector3 SunAngles = new Vector3(40f, -20f, 0f);
        private static readonly Vector3 FillAngles = new Vector3(10f, 150f, 0f);

        [MenuItem(Menu)]
        private static void Render()
        {
            ItemSpec[] specs = Selection.GetFiltered<ItemSpec>(SelectionMode.Assets);
            int drawn = 0;

            foreach (ItemSpec spec in specs)
            {
                if (spec.Model == null)
                {
                    Debug.LogWarning($"{spec.name} has no model, its icon stays as it is");
                    continue;
                }

                if (Draw(spec)) drawn++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Rendered {drawn} icons out of {specs.Length} selected specs");
        }

        [MenuItem(Menu, true)]
        private static bool Selected()
        {
            return Selection.GetFiltered<ItemSpec>(SelectionMode.Assets).Length > 0;
        }

        private static bool Draw(ItemSpec spec)
        {
            GameObject studio = new GameObject("Icon Studio") { hideFlags = HideFlags.HideAndDontSave };
            GameObject model = Object.Instantiate(spec.Model, studio.transform);

            try
            {
                Dress(model);

                if (!Framed(model, out Bounds bounds))
                {
                    Debug.LogWarning($"{spec.name} has a model without renderers, its icon stays as it is");
                    return false;
                }

                Camera camera = Studio(studio, bounds);
                Texture2D shot = Shoot(camera, camera.GetComponent<HDAdditionalCameraData>());
                string path = Save(spec, shot);

                Object.DestroyImmediate(shot);

                return Attach(spec, path);
            }
            finally
            {
                Object.DestroyImmediate(studio);
            }
        }

        private static void Dress(GameObject model)
        {
            foreach (Transform part in model.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = Layer;
        }

        private static bool Framed(GameObject model, out Bounds bounds)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            bounds = default;

            if (renderers.Length == 0) return false;

            bounds = renderers[0].bounds;

            for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);

            return true;
        }

        private static Camera Studio(GameObject studio, Bounds bounds)
        {
            Sun(studio, SunAngles, SunIntensity);
            Sun(studio, FillAngles, FillIntensity);
            Sky(studio);

            var holder = new GameObject("Camera") { layer = Layer };
            holder.transform.SetParent(studio.transform);

            Camera camera = holder.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Framing(bounds);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = Distance * 4f;
            camera.cullingMask = 1 << Layer;
            camera.transform.rotation = Quaternion.Euler(SightAngles);
            camera.transform.position = bounds.center - camera.transform.forward * Distance;

            HDAdditionalCameraData data = holder.AddComponent<HDAdditionalCameraData>();
            data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            data.backgroundColorHDR = new Color(0f, 0f, 0f, 0f);
            data.volumeLayerMask = 1 << Layer;
            data.probeLayerMask = 0;

            return camera;
        }

        private static float Framing(Bounds bounds)
        {
            Quaternion sight = Quaternion.Euler(SightAngles);
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

            return widest * Padding;
        }

        private static void Sun(GameObject studio, Vector3 angles, float intensity)
        {
            var holder = new GameObject("Light") { layer = Layer };
            holder.transform.SetParent(studio.transform);
            holder.transform.rotation = Quaternion.Euler(angles);

            Light light = holder.AddComponent<Light>();
            light.type = LightType.Directional;
            light.cullingMask = 1 << Layer;

            HDAdditionalLightData data = holder.AddComponent<HDAdditionalLightData>();
            data.EnableShadows(false);

            light.lightUnit = LightUnit.Lux;
            light.intensity = intensity;
        }

        private static void Sky(GameObject studio)
        {
            var holder = new GameObject("Volume") { layer = Layer };
            holder.transform.SetParent(studio.transform);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;

            var exposure = profile.Add<Exposure>();
            exposure.mode.overrideState = true;
            exposure.mode.value = ExposureMode.Fixed;
            exposure.fixedExposure.overrideState = true;
            exposure.fixedExposure.value = Stop;

            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            Volume volume = holder.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.profile = profile;
        }

        private static Texture2D Shoot(Camera camera, HDAdditionalCameraData data)
        {
            Texture2D onBlack = Frame(camera, data, Color.black);
            Texture2D onWhite = Frame(camera, data, Color.white);

            Texture2D shot = Cut(onBlack, onWhite);

            Object.DestroyImmediate(onBlack);
            Object.DestroyImmediate(onWhite);

            return shot;
        }

        private static Texture2D Frame(Camera camera, HDAdditionalCameraData data, Color background)
        {
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;

            data.backgroundColorHDR = background;
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;

            var shot = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            shot.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
            shot.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(target);

            return shot;
        }

        private static Texture2D Cut(Texture2D onBlack, Texture2D onWhite)
        {
            Color[] dark = onBlack.GetPixels();
            Color[] light = onWhite.GetPixels();
            var pixels = new Color[dark.Length];

            float span = Mathf.Max(light[0].r - dark[0].r, light[0].g - dark[0].g, light[0].b - dark[0].b);

            if (span <= 0.01f)
            {
                Debug.LogWarning("Icon studio rendered the same picture on both backgrounds, the icon stays opaque");
                span = 1f;
            }

            for (int index = 0; index < dark.Length; index++)
            {
                Color over = dark[index];
                float lifted = (light[index].r - over.r + light[index].g - over.g + light[index].b - over.b) / 3f;
                float alpha = Mathf.Clamp01(1f - lifted / span);

                pixels[index] = alpha <= 0.004f
                    ? Color.clear
                    : new Color(over.r / alpha, over.g / alpha, over.b / alpha, alpha);
            }

            var shot = new Texture2D(onBlack.width, onBlack.height, TextureFormat.RGBA32, false);
            shot.SetPixels(pixels);
            shot.Apply();

            return shot;
        }

        private static string Save(ItemSpec spec, Texture2D shot)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(spec));
            string path = Path.Combine(folder, spec.name + "Icon.png").Replace('\\', '/');

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

        private static bool Attach(ItemSpec spec, string path)
        {
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (icon == null)
            {
                Debug.LogWarning($"{spec.name} rendered into {path}, but the sprite did not import");
                return false;
            }

            var serialized = new SerializedObject(spec);
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.ApplyModifiedProperties();

            return true;
        }
    }
}
