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

        public abstract void Save(string path, int width = 0, int height = 0);
    }
}
