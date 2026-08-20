using System.Collections;
using System.IO;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Screenshots
{
    public class HDRPScreenshotManager : ScreenshotManager
    {
        private static readonly Journal Log = Logs.Here();

        public override IEnumerator WriteCoroutine(string path, ScreenshotSetting setting)
        {
            yield return new WaitForEndOfFrame();

            Log.Info($"Entity {name} is making screenshot {setting.Width} x {setting.Height} p {path} q {setting.Quality}...");

            Texture2D fullScreenshot = ScreenCapture.CaptureScreenshotAsTexture(1);

            if (fullScreenshot == null)
            {
                Log.Warn($"Entity {name} failed to capture screenshot texture");
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
            Destroy(fullScreenshot);

            byte[] bytes = previewTexture.EncodeToJPG((setting.Quality <= 0) ? 80 : setting.Quality);
            Destroy(previewTexture);

            string finalPath = Path.Combine(Config.Root(), path);
            string directoryPath = Path.GetDirectoryName(finalPath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllBytes(finalPath, bytes);
            Log.Info($"Entity {name} wrote screenshot {targetWidth} x {targetHeight} to {finalPath}");
        }
    }
}
