using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Editor
{
    public struct IconSetup
    {
        public Vector3 SightAngles;
        public Vector3 KeyAngles;
        public Vector3 FillAngles;
        public float KeyIntensity;
        public float FillIntensity;
        public float Stop;
        public float Padding;
        public int Size;
    }

    public static class IconStudio
    {
        private const int Layer = 11;
        private const float Distance = 10f;

        public static IconSetup Default()
        {
            return new IconSetup
            {
                SightAngles = new Vector3(18f, -35f, 0f),
                KeyAngles = new Vector3(40f, -20f, 0f),
                FillAngles = new Vector3(10f, 150f, 0f),
                KeyIntensity = 20000f,
                FillIntensity = 6000f,
                Stop = 13.2f,
                Padding = 1.12f,
                Size = 512
            };
        }

        public static string Shoot(GameObject prefab, IconSetup setup)
        {
            var studio = new GameObject("Icon Studio") { hideFlags = HideFlags.HideAndDontSave };
            GameObject model = Object.Instantiate(prefab, studio.transform);

            try
            {
                Dress(model);

                if (!Framed(model, out Bounds bounds))
                {
                    Debug.LogWarning($"{prefab.name} has no renderers, nothing to shoot");
                    return null;
                }

                Camera camera = Setup(studio, bounds, setup);
                Texture2D shot = Shoot(camera, camera.GetComponent<HDAdditionalCameraData>(), setup.Size);
                string path = Save(prefab, shot);

                Object.DestroyImmediate(shot);

                return path;
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

        private static Camera Setup(GameObject studio, Bounds bounds, IconSetup setup)
        {
            Sun(studio, setup.KeyAngles, setup.KeyIntensity);
            Sun(studio, setup.FillAngles, setup.FillIntensity);
            Sky(studio, setup.Stop);

            var holder = new GameObject("Camera") { layer = Layer };
            holder.transform.SetParent(studio.transform);

            Camera camera = holder.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Framing(bounds, setup);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = Distance * 4f;
            camera.cullingMask = 1 << Layer;
            camera.transform.rotation = Quaternion.Euler(setup.SightAngles);
            camera.transform.position = bounds.center - camera.transform.forward * Distance;

            HDAdditionalCameraData data = holder.AddComponent<HDAdditionalCameraData>();
            data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            data.volumeLayerMask = 1 << Layer;
            data.probeLayerMask = 0;

            return camera;
        }

        private static float Framing(Bounds bounds, IconSetup setup)
        {
            var sight = Quaternion.Euler(setup.SightAngles);
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

            return widest * setup.Padding;
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

        private static void Sky(GameObject studio, float stop)
        {
            var holder = new GameObject("Volume") { layer = Layer };
            holder.transform.SetParent(studio.transform);

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;

            Exposure exposure = profile.Add<Exposure>();
            exposure.mode.overrideState = true;
            exposure.mode.value = ExposureMode.Fixed;
            exposure.fixedExposure.overrideState = true;
            exposure.fixedExposure.value = stop;

            Tonemapping tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            Volume volume = holder.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.profile = profile;
        }

        private static Texture2D Shoot(Camera camera, HDAdditionalCameraData data, int size)
        {
            Texture2D onBlack = Frame(camera, data, Color.black, size);
            Texture2D onWhite = Frame(camera, data, Color.white, size);

            Texture2D full = Cut(onBlack, onWhite);
            Texture2D shot = Crop(full);

            if (shot != full) Object.DestroyImmediate(full);

            Object.DestroyImmediate(onBlack);
            Object.DestroyImmediate(onWhite);

            return shot;
        }

        private static Texture2D Frame(Camera camera, HDAdditionalCameraData data, Color background, int size)
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

        private static Texture2D Cut(Texture2D onBlack, Texture2D onWhite)
        {
            Color[] dark = onBlack.GetPixels();
            Color[] light = onWhite.GetPixels();
            var pixels = new Color[dark.Length];

            float span = Mathf.Max(light[0].r - dark[0].r, light[0].g - dark[0].g, light[0].b - dark[0].b);

            if (span <= 0.01f)
            {
                Debug.LogWarning("Both backgrounds came out the same, the icon stays opaque");
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

        private static Texture2D Crop(Texture2D shot)
        {
            Color[] pixels = shot.GetPixels();
            int left = shot.width;
            int right = -1;
            int bottom = shot.height;
            int top = -1;

            for (int y = 0; y < shot.height; y++)
            for (int x = 0; x < shot.width; x++)
            {
                if (pixels[y * shot.width + x].a <= 0.02f) continue;

                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
            }

            if (right < 0)
            {
                Debug.LogWarning("The shot came out fully transparent, nothing to crop");
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
