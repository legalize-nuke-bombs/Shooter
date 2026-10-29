using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Shooter.Infrastructure
{
    public class ClientConfig : INotifyBindablePropertyChanged
    {
        private string name = "Player";
        private string invite = "";
        private float master = 1f;
        private float music = 0.25f;
        private float ambience = 1f;
        private float sounds = 1f;
        private string screenResolution = "";
        private string upscaler = Upscalers.Default;
        private string renderScale = RenderScales.Default;
        private string antialiasing = Antialiasings.Default;
        private string fog = FogQualities.Default;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        [CreateProperty]
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        [CreateProperty]
        public string Invite
        {
            get => invite;
            set => SetProperty(ref invite, value);
        }

        [CreateProperty]
        public float Master
        {
            get => master;
            set => SetProperty(ref master, value);
        }

        [CreateProperty]
        public float Music
        {
            get => music;
            set => SetProperty(ref music, value);
        }

        [CreateProperty]
        public float Ambience
        {
            get => ambience;
            set => SetProperty(ref ambience, value);
        }

        [CreateProperty]
        public float Sounds
        {
            get => sounds;
            set => SetProperty(ref sounds, value);
        }

        [CreateProperty]
        public string ScreenResolution
        {
            get => screenResolution;
            set => SetProperty(ref screenResolution, value);
        }

        [CreateProperty]
        public string Upscaler
        {
            get => upscaler;
            set => SetProperty(ref upscaler, Upscalers.Keys.Contains(value) ? value : Upscalers.Default);
        }

        [CreateProperty]
        public string RenderScale
        {
            get => renderScale;
            set => SetProperty(ref renderScale, RenderScales.Keys.Contains(value) ? value : RenderScales.Default);
        }

        [CreateProperty]
        public string Antialiasing
        {
            get => antialiasing;
            set => SetProperty(ref antialiasing, Antialiasings.Keys.Contains(value) ? value : Antialiasings.Default);
        }

        [CreateProperty]
        public string Fog
        {
            get => fog;
            set => SetProperty(ref fog, FogQualities.Keys.Contains(value) ? value : FogQualities.Default);
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string property = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;

            field = value;
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}
