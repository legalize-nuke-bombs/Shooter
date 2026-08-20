using System;
using UnityEngine;

namespace Shooter.Game.Core.Screenshots
{
    [Serializable]
    public struct ScreenshotSetting
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private int quality;

        public int Width => width;
        public int Height => height;
        public int Quality => quality;
    }
}
