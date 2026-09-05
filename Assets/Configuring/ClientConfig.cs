using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Shooter.Configuring
{
    public class ClientConfig : INotifyBindablePropertyChanged
    {
        private string name = "Player";
        private string invite = "";
        private float master = 1f;
        private float music = 0.25f;
        private float ambience = 1f;
        private float sounds = 1f;
        private float vhs = 0.35f;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        [CreateProperty]
        public string Name
        {
            get => name;
            set => Change(ref name, value);
        }

        [CreateProperty]
        public string Invite
        {
            get => invite;
            set => Change(ref invite, value);
        }

        [CreateProperty]
        public float Master
        {
            get => master;
            set => Change(ref master, value);
        }

        [CreateProperty]
        public float Music
        {
            get => music;
            set => Change(ref music, value);
        }

        [CreateProperty]
        public float Ambience
        {
            get => ambience;
            set => Change(ref ambience, value);
        }

        [CreateProperty]
        public float Sounds
        {
            get => sounds;
            set => Change(ref sounds, value);
        }

        [CreateProperty]
        public float Vhs
        {
            get => vhs;
            set => Change(ref vhs, value);
        }

        private void Change<T>(ref T field, T value, [CallerMemberName] string property = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;

            field = value;
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}
