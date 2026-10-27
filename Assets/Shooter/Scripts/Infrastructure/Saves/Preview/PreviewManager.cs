using System.Collections;

namespace Shooter.Infrastructure
{
    public static class PreviewManager
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly ScreenshotSetting PreviewSetting = new(640, 360, 80);

        public static IEnumerator WritePreviewCoroutine(string path)
        {
            Log.Info($"Writing preview to {path}...");
            yield return Screenshotter.Instance.TakeScreenshot(path, PreviewSetting);
        }
    }
}
