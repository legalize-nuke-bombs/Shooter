using System.Collections;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Infrastructure
{
    public static class Screenshotter
    {
        private static readonly Journal Log = Logs.Here();

        public static IEnumerator TakeScreenshot(string path, ScreenshotSetting setting)
        {
            yield return new WaitForEndOfFrame();

            Texture2D capturedTexture = ScreenCapture.CaptureScreenshotAsTexture(1);
            if (capturedTexture == null)
            {
                Log.Warn("Failed to capture screenshot texture");
                yield break;
            }

            int width = setting.Width <= 0 ? capturedTexture.width : setting.Width;
            int height = setting.Height <= 0 ? capturedTexture.height : setting.Height;

            Texture2D resizedTexture = new(width, height, TextureFormat.RGB24, false);
            RenderTexture scaledRenderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(capturedTexture, scaledRenderTexture);

            RenderTexture previousActiveRenderTexture = RenderTexture.active;
            RenderTexture.active = scaledRenderTexture;
            resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resizedTexture.Apply();
            RenderTexture.active = previousActiveRenderTexture;

            RenderTexture.ReleaseTemporary(scaledRenderTexture);
            Object.Destroy(capturedTexture);

            byte[] jpgBytes = resizedTexture.EncodeToJPG(setting.Quality);
            Object.Destroy(resizedTexture);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, jpgBytes);
            Log.Info($"Wrote screenshot {width} x {height} to {path}");
        }
    }
}
