using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Shooter.Game.Core.Mixing
{
    public static class Mixer
    {
        private const string Asset = "Mixer";
        private const string MasterVolume = "MasterVolume";
        private const string MusicVolume = "MusicVolume";
        private const string AmbienceVolume = "AmbienceVolume";
        private const string SoundsVolume = "SoundsVolume";
        private const float Silence = -80f;
        private const float DecibelsPerDecade = 20f;
        private static readonly Journal Log = Logs.Here();

        private static AudioMixer mixer;
        private static ClientConfig client;

        // Sounds find their group in their own spec; the mixer only has to follow the volume settings
        public static void Tune()
        {
            if (mixer != null) return;

            mixer = Resources.Load<AudioMixer>(Asset);
            if (mixer == null)
            {
                Log.Error($"No {Asset} mixer in Resources, the volume settings stay unheard");
                return;
            }

            client = Config.Read().Client;
            client.propertyChanged += Changed;
            Apply();
        }

        private static void Changed(object sender, BindablePropertyChangedEventArgs args)
        {
            Apply();
        }

        private static void Apply()
        {
            Set(MasterVolume, client.Master);
            Set(MusicVolume, client.Music);
            Set(AmbienceVolume, client.Ambience);
            Set(SoundsVolume, client.Sounds);
        }

        private static void Set(string parameter, float volume)
        {
            float decibels = volume <= 0f ? Silence : Mathf.Max(Silence, DecibelsPerDecade * Mathf.Log10(volume));
            if (!mixer.SetFloat(parameter, decibels)) Log.Warn($"Mixer {mixer.name} exposes no {parameter}, that volume stays as is");
        }
    }
}
