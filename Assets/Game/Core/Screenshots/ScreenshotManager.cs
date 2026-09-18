using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Game.Core.Screenshots
{
    public static class ScreenshotManager
    {
        private const string Folder = "Screenshots";
        private const string Prefix = "Screenshot";
        private const string StampFormat = "yyyy_MM_dd_HH_mm_ss";
        private const int DefaultQuality = 80;
        private static readonly Journal Log = Logs.Here();
        private static readonly ScreenshotSetting Whole = new(0, 0, 90);

        public static IEnumerator ShootCoroutine()
        {
            string stamp = DateTime.Now.ToString(StampFormat, CultureInfo.InvariantCulture);
            string path = Path.Combine(Config.Root(), Folder, Prefix + "_" + stamp + ".jpg");

            yield return WriteCoroutine(path, Whole);
        }

        public static IEnumerator WriteCoroutine(string path, ScreenshotSetting setting)
        {
            yield return new WaitForEndOfFrame();

            Log.Info($"Making screenshot {setting.Width} x {setting.Height} p {path} q {setting.Quality}...");

            Texture2D fullScreenshot = ScreenCapture.CaptureScreenshotAsTexture(1);

            if (fullScreenshot == null)
            {
                Log.Warn("Failed to capture screenshot texture");
                yield break;
            }

            int targetWidth = (setting.Width <= 0) ? fullScreenshot.width : setting.Width;
            int targetHeight = (setting.Height <= 0) ? fullScreenshot.height : setting.Height;

            var previewTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);

            var rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0);
            Graphics.Blit(fullScreenshot, rt);

            RenderTexture oldActive = RenderTexture.active;
            RenderTexture.active = rt;

            previewTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            previewTexture.Apply();

            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(rt);
            Object.Destroy(fullScreenshot);

            byte[] bytes = previewTexture.EncodeToJPG((setting.Quality <= 0) ? DefaultQuality : setting.Quality);
            Object.Destroy(previewTexture);

            string directoryPath = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllBytes(path, bytes);
            Log.Info($"Wrote screenshot {targetWidth} x {targetHeight} to {path}");
        }
    }
}
