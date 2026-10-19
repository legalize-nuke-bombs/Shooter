using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public static class Mixer
    {
        private const string MixerResourcePath = "Mixer";
        private const string MasterVolume = "MasterVolume";
        private const string MusicVolume = "MusicVolume";
        private const string AmbienceVolume = "AmbienceVolume";
        private const string SoundsVolume = "SoundsVolume";
        private const float SilenceDecibels = -80f;
        private const float DecibelsPerDecade = 20f;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private static AudioMixer mixer;
        private static ClientConfig clientConfig;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadMixerAndFollowVolumeSettings()
        {
            if (mixer != null) return;

            mixer = Resources.Load<AudioMixer>(MixerResourcePath);
            if (mixer == null)
            {
                Log.Error($"No {MixerResourcePath} mixer in Resources, the volume settings stay unheard");
                return;
            }

            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            ApplyVolumeSettings();
        }

        private static void HandleClientConfigChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            ApplyVolumeSettings();
        }

        private static void ApplyVolumeSettings()
        {
            SetMixerVolume(MasterVolume, clientConfig.Master);
            SetMixerVolume(MusicVolume, clientConfig.Music);
            SetMixerVolume(AmbienceVolume, clientConfig.Ambience);
            SetMixerVolume(SoundsVolume, clientConfig.Sounds);
        }

        private static void SetMixerVolume(string parameter, float volume)
        {
            float decibels = volume <= 0f ? SilenceDecibels : Mathf.Max(SilenceDecibels, DecibelsPerDecade * Mathf.Log10(volume));
            if (!mixer.SetFloat(parameter, decibels)) Log.Error($"Mixer {mixer.name} exposes no {parameter}, that volume stays as is");
        }
    }
}
