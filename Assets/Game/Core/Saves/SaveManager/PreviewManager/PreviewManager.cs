using System.Collections;
using Shooter.Game.Core.Screenshots;
using Shooter.Logging;

namespace Shooter.Game.Core.Saves
{
    public static class PreviewManager
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly ScreenshotSetting Setting = new(640, 360, 80);

        public static IEnumerator WriteCoroutine(string path)
        {
            Log.Info($"Writing preview to {path}...");
            yield return ScreenshotManager.WriteCoroutine(path, Setting);
        }
    }
}
