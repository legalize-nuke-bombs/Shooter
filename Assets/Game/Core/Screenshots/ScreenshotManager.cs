using System;
using System.Collections;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Screenshots
{
    public abstract class ScreenshotManager : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static ScreenshotManager Current { get; private set; }

        protected virtual void Awake()
        {
            if (Current != null)
            {
                Log.Error("Singleton class has more than one instance");
            }
            Current = this;
        }

        protected void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public abstract IEnumerator WriteCoroutine(string path, ScreenshotSetting setting);
    }
}
