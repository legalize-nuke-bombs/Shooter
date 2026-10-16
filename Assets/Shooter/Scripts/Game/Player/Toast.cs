namespace Shooter.Game
{
    public readonly struct Toast
    {
        public Toast(IconSpec icon, EarSoundSpec sound, string title, string subtitle)
        {
            Icon = icon;
            Sound = sound;
            Title = title;
            Subtitle = subtitle;
        }

        public IconSpec Icon { get; }

        public EarSoundSpec Sound { get; }

        public string Title { get; }

        public string Subtitle { get; }
    }
}
