using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class EarSpeaker : NetworkBehaviour
    {
        private const int MaxVoices = 4;
        private static readonly Journal Log = Logs.Here();

        private readonly List<AudioSource> audioSources = new();

        private int nextAudioSourceIndexToReuse;

        public void PlayForOwner(EarSoundAsset sound)
        {
            if (!IsServer) return;
            if (GetComponent<Player>() == null) return;

            if (sound == null)
            {
                Log.Warn($"Entity {name} was asked to play an ear sound without an asset set");
                return;
            }

            PlayForOwnerRpc(sound.NetworkId, sound.PickRandomVariant());
        }

        public void PlayLocally(EarSoundAsset sound)
        {
            if (sound == null) return;

            PlayVariantLocally(sound, sound.PickRandomVariant());
        }

        [Rpc(SendTo.Owner)]
        private void PlayForOwnerRpc(FixedString32Bytes id, byte variant)
        {
            EarSoundCatalog earSoundCatalog = Catalogs.GetCatalog<EarSoundCatalog>();
            if (earSoundCatalog == null)
            {
                Log.Warn($"Entity {name} cannot play {id} in the ear: the world has no ear sound catalog");
                return;
            }

            EarSoundAsset sound = earSoundCatalog.GetAssetById(id);
            if (sound == null) return;

            PlayVariantLocally(sound, variant);
        }

        private void PlayVariantLocally(EarSoundAsset sound, byte variant)
        {
            AudioClip clip = sound.GetClip(variant);
            if (clip == null) return;

            AudioSource audioSource = TakeAudioSource();
            audioSource.clip = clip;
            audioSource.outputAudioMixerGroup = sound.Output;
            audioSource.volume = sound.Volume;
            audioSource.pitch = 1f + Random.Range(-sound.PitchVariation, sound.PitchVariation);

            Log.Info($"Entity {name} plays {sound.Id} variant {variant} in the owner's ear at t={Time.time}");

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
            audioSource.spatialBlend = 0f;
            audioSources.Add(audioSource);

            return audioSource;
        }
    }
}
