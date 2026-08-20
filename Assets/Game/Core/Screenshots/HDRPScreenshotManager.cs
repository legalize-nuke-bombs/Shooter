using System.Collections;
using System.IO;
using Shooter.Configuring;
using UnityEngine;

namespace Shooter.Game.Core.Screenshots
{
    public class HDRPScreenshotManager : ScreenshotManager
    {
        public override void Save(string path, int width = 0, int height = 0)
        {
            StartCoroutine(SaveCoroutine(path, width, height));
        }

        private IEnumerator SaveCoroutine(string path, int width, int height)
        {
            Texture2D fullScreenshot = ScreenCapture.CaptureScreenshotAsTexture(1);
            yield return null;

            if (fullScreenshot == null)
            {
                Debug.LogWarning("Failed to capture screenshot texture");
                yield break;
            }

            int targetWidth = (width <= 0) ? fullScreenshot.width : width;
            int targetHeight = (height <= 0) ? fullScreenshot.height : height;

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

            byte[] bytes = previewTexture.EncodeToJPG(70);
            Destroy(previewTexture);

            string finalPath = Path.Combine(Config.Root(), path);
            string directoryPath = Path.GetDirectoryName(finalPath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllBytes(finalPath, bytes);
            Debug.Log($"Screenshot saved to: {finalPath}");
        }

        private float timer = 0;
        private float timerInterval = 5f;

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= timerInterval)
            {
                Save("Screenshot/123.jpg", 480, 270);
                timer = 0;
            }
        }
    }
}
