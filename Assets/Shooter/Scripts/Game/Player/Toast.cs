namespace Shooter.Game
{
    public readonly struct Toast
    {
        public Toast(IconAsset icon, EarSoundAsset sound, string title, string subtitle)
        {
            Icon = icon;
            Sound = sound;
            Title = title;
            Subtitle = subtitle;
        }

        public IconAsset Icon { get; }

        public EarSoundAsset Sound { get; }

        public string Title { get; }

        public string Subtitle { get; }
    }
}
