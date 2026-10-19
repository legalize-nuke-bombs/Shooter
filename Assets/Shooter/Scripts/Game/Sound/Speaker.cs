using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Speaker : NetworkBehaviour
    {
        private const int MaxVoices = 8;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private readonly List<AudioSource> audioSources = new();

        private int nextAudioSourceIndexToReuse;

        public void PlayForEveryone(SoundAsset sound, float gain = 1f)
        {
            if (!IsServer) return;

            if (sound == null)
            {
                Log.Warn($"Entity {name} was asked to play a sound without an asset set");
                return;
            }

            PlayForEveryoneRpc(sound.NetworkId, sound.PickRandomVariant(), gain);
        }

        [Rpc(SendTo.Everyone)]
        private void PlayForEveryoneRpc(FixedString32Bytes id, byte variant, float gain)
        {
            SoundCatalog soundCatalog = Catalogs.GetCatalog<SoundCatalog>();
            if (soundCatalog == null)
            {
                Log.Warn($"Entity {name} cannot play {id}: the world has no sound catalog");
                return;
            }

            SoundAsset sound = soundCatalog.GetAssetById(id);
            if (sound == null) return;

            AudioClip clip = sound.GetClip(variant);
            if (clip == null) return;

            AudioSource audioSource = TakeAudioSource();
            audioSource.clip = clip;
            audioSource.outputAudioMixerGroup = sound.Output;
            audioSource.volume = sound.Volume * gain;
            audioSource.pitch = 1f + Random.Range(-sound.PitchVariation, sound.PitchVariation);
            audioSource.rolloffMode = sound.Rolloff;
            audioSource.minDistance = sound.MinDistance;
            audioSource.maxDistance = sound.MaxDistance;

            audioSource.Play();
        }

        private AudioSource TakeAudioSource()
        {
            foreach (AudioSource audioSource in audioSources)
            {
                if (!audioSource.isPlaying) return audioSource;
            }

            if (audioSources.Count < MaxVoices) return AddAudioSource();

            AudioSource reusedAudioSource = audioSources[nextAudioSourceIndexToReuse];
            nextAudioSourceIndexToReuse = (nextAudioSourceIndexToReuse + 1) % audioSources.Count;

            return reusedAudioSource;
        }

        private AudioSource AddAudioSource()
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSources.Add(audioSource);

            return audioSource;
        }
    }
}
