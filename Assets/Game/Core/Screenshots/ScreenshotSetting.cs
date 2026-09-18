namespace Shooter.Game.Core.Screenshots
{
    public readonly struct ScreenshotSetting
    {
        public ScreenshotSetting(int width, int height, int quality)
        {
            Width = width;
            Height = height;
            Quality = quality;
        }

        public int Width { get; }
        public int Height { get; }
        public int Quality { get; }
    }
}
