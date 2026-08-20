using System.Collections;
using UnityEngine;

namespace Shooter.Game.Core.Screenshots
{
    public abstract class ScreenshotManager : MonoBehaviour
    {
        public static ScreenshotManager Current { get; private set; }

        protected virtual void Awake()
        {
            Current = this;
        }

        public abstract IEnumerator WriteCoroutine(string path, ScreenshotSetting setting);
    }
}
