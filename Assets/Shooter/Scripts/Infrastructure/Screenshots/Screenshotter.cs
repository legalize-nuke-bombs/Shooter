using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Infrastructure
{
    public static class Screenshotter
    {
        private const string Folder = "Screenshots";
        private const string Prefix = "Screenshot";
        private const string StampFormat = "yyyy_MM_dd_HH_mm_ss";
        private static readonly Journal Log = Logs.Here();
        private static readonly ScreenshotSetting Whole = new(0, 0, 90);

        public static IEnumerator TakeToScreenshotsFolder()
        {
            string stamp = DateTime.Now.ToString(StampFormat, CultureInfo.InvariantCulture);
            string path = Path.Combine(Config.Root(), Folder, Prefix + "_" + stamp + ".jpg");

            yield return Take(path, Whole);
        }

        public static IEnumerator Take(string path, ScreenshotSetting setting)
        {
            yield return new WaitForEndOfFrame();

            Texture2D captured = ScreenCapture.CaptureScreenshotAsTexture(1);
            if (captured == null)
            {
                Log.Warn("Failed to capture screenshot texture");
                yield break;
            }

            int width = setting.Width <= 0 ? captured.width : setting.Width;
            int height = setting.Height <= 0 ? captured.height : setting.Height;

            Texture2D resized = new(width, height, TextureFormat.RGB24, false);
            RenderTexture scaled = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(captured, scaled);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = scaled;
            resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resized.Apply();
            RenderTexture.active = previousActive;

            RenderTexture.ReleaseTemporary(scaled);
            Object.Destroy(captured);

            byte[] bytes = resized.EncodeToJPG(setting.Quality);
            Object.Destroy(resized);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
            Log.Info($"Wrote screenshot {width} x {height} to {path}");
        }
    }
}
