using System.Collections;

namespace Shooter.Infrastructure
{
    public static class PreviewManager
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly ScreenshotSetting Setting = new(640, 360, 80);

        public static IEnumerator WriteCoroutine(string path)
        {
            Log.Info($"Writing preview to {path}...");
            yield return Screenshotter.Take(path, Setting);
        }
    }
}
