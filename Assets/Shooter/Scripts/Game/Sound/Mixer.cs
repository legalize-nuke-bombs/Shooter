using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class Mixer : MonoBehaviour
    {
        private const string MasterVolume = "MasterVolume";
        private const string MusicVolume = "MusicVolume";
        private const string AmbienceVolume = "AmbienceVolume";
        private const string SoundsVolume = "SoundsVolume";
        private const float SilenceDecibels = -80f;
        private const float DecibelsPerDecade = 20f;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private AudioMixer mixer;

        private ClientConfig clientConfig;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
        }

        private void OnDisable()
        {
            clientConfig.propertyChanged -= HandleClientConfigChanged;
        }

        // The mixer puts its start snapshot over exposed parameters set before the first frame, so volumes go in from Start on
        private void Start()
        {
            ApplyVolumeSettings();
        }

        private void HandleClientConfigChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            ApplyVolumeSettings();
        }

        private void ApplyVolumeSettings()
        {
            SetMixerVolume(MasterVolume, clientConfig.Master);
            SetMixerVolume(MusicVolume, clientConfig.Music);
            SetMixerVolume(AmbienceVolume, clientConfig.Ambience);
            SetMixerVolume(SoundsVolume, clientConfig.Sounds);
        }

        private void SetMixerVolume(string parameter, float volume)
        {
            float decibels = volume <= 0f ? SilenceDecibels : Mathf.Max(SilenceDecibels, DecibelsPerDecade * Mathf.Log10(volume));
            if (!mixer.SetFloat(parameter, decibels)) Log.Error($"Mixer {mixer.name} exposes no {parameter}, that volume stays as is");
        }
    }
}
