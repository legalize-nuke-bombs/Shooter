using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game
{
    public static class WorldSpeaker
    {
        private const int MaxVoices = 16;

        private static readonly List<AudioSource> AudioSources = new();

        private static Transform holder;
        private static int nextAudioSourceIndexToReuse;

        public static void PlayLocallyAt(SoundAsset sound, byte variant, Vector3 position, float gain = 1f)
        {
            if (sound == null || sound.GetClip(variant) == null) return;

            AudioSource audioSource = TakeAudioSource();
            audioSource.transform.position = position;
            if (sound.TryConfigure(audioSource, variant, gain)) audioSource.Play();
        }

        private static AudioSource TakeAudioSource()
        {
            if (holder == null)
            {
                holder = new GameObject("World Speaker").transform;
                AudioSources.Clear();
                nextAudioSourceIndexToReuse = 0;
            }

            foreach (AudioSource audioSource in AudioSources)
            {
                if (!audioSource.isPlaying) return audioSource;
            }

            if (AudioSources.Count < MaxVoices) return AddAudioSource();

            AudioSource reusedAudioSource = AudioSources[nextAudioSourceIndexToReuse];
            nextAudioSourceIndexToReuse = (nextAudioSourceIndexToReuse + 1) % AudioSources.Count;

            return reusedAudioSource;
        }

        private static AudioSource AddAudioSource()
        {
            AudioSource audioSource = new GameObject("Voice").AddComponent<AudioSource>();
            audioSource.transform.SetParent(holder, false);
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.dopplerLevel = 0f;
            AudioSources.Add(audioSource);

            return audioSource;
        }
    }
}
