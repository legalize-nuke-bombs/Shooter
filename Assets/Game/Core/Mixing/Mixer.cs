using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Shooter.Game.Core.Mixing
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class Mixer : MonoBehaviour
    {
        private const string MasterVolume = "MasterVolume";
        private const string MusicVolume = "MusicVolume";
        private const string AmbienceVolume = "AmbienceVolume";
        private const string SoundsVolume = "SoundsVolume";
        private const float Silence = -80f;
        private const float DecibelsPerDecade = 20f;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup music;
        [SerializeField] private AudioMixerGroup ambience;
        [SerializeField] private AudioMixerGroup sounds;

        private ClientConfig client;

        public static Mixer Current { get; private set; }

        public static AudioMixerGroup Music => Current == null ? null : Current.music;

        public static AudioMixerGroup Ambience => Current == null ? null : Current.ambience;

        public static AudioMixerGroup Sounds => Current == null ? null : Current.sounds;

        private void Awake()
        {
            if (mixer == null || music == null || ambience == null || sounds == null)
                Log.Error("Mixer prefab misses the mixer or a group, sounds will bypass the volume settings");

            if (Current != null)
            {
                Log.Error("Singleton class has more than one instance");
            }
            Current = this;
        }

        private void OnEnable()
        {
            client = Config.Read().Client;
            client.propertyChanged += Changed;
        }

        private void Start()
        {
            Apply();
        }

        private void OnDisable()
        {
            client.propertyChanged -= Changed;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Changed(object sender, BindablePropertyChangedEventArgs args)
        {
            Apply();
        }

        private void Apply()
        {
            if (mixer == null) return;

            Set(MasterVolume, client.Master);
            Set(MusicVolume, client.Music);
            Set(AmbienceVolume, client.Ambience);
            Set(SoundsVolume, client.Sounds);
        }

        private void Set(string parameter, float volume)
        {
            float decibels = volume <= 0f ? Silence : Mathf.Max(Silence, DecibelsPerDecade * Mathf.Log10(volume));
            if (!mixer.SetFloat(parameter, decibels)) Log.Warn($"Mixer {mixer.name} exposes no {parameter}, that volume stays as is");
        }
    }
}
