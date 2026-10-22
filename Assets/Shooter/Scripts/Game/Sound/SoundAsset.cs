using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Audio;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Sound", fileName = "Sound")]
    public class SoundAsset : Asset
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private AudioClip[] clips;

        [SerializeField] private AudioMixerGroup output;

        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

        [SerializeField] private AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;

        [SerializeField] private float minDistance = 1f;

        [SerializeField] private float maxDistance = 100f;

        [SerializeField] [Range(0f, 0.5f)] private float pitchVariation = 0.05f;

        public AudioMixerGroup Output => output;

        public float Volume => volume;

        public AudioRolloffMode Rolloff => rolloff;

        public float MinDistance => minDistance;

        public float MaxDistance => maxDistance;

        public float PitchVariation => pitchVariation;

        public bool IsSilent => clips == null || clips.Length == 0;

        private void OnEnable()
        {
            if (output == null) Log.Error($"Sound {name} has no mixer group set, it will not follow the volume settings");
        }

        public byte PickRandomVariant()
        {
            return IsSilent ? (byte)0 : (byte)Random.Range(0, clips.Length);
        }

        public AudioClip GetClip(byte variant)
        {
            return IsSilent ? null : clips[variant % clips.Length];
        }

        public bool TryConfigure(AudioSource audioSource, byte variant, float gain)
        {
            AudioClip clip = GetClip(variant);
            if (clip == null) return false;

            audioSource.clip = clip;
            audioSource.outputAudioMixerGroup = output;
            audioSource.volume = volume * gain;
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.rolloffMode = rolloff;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            return true;
        }
    }
}
