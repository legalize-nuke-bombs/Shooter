using Shooter.Game.Body;
using Shooter.Game.Core;

namespace Shooter.Client.Interface
{
    // What the corner feed shows: a picture, a sound in the ears and two lines, all composed by whoever asks
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
