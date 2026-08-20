using System.Collections;
using Shooter.Game.Core.Screenshots;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public class PreviewManager : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private ScreenshotSetting setting;

        public IEnumerator WriteCoroutine(string path)
        {
            Log.Info($"Entity {name} is writing preview to {path}...");
            yield return StartCoroutine(ScreenshotManager.Current.WriteCoroutine(path, setting));
        }
    }
}
