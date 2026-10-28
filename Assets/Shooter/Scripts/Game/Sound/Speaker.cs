using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class Speaker : ShooterNetworkComponent
    {
        private const int MaxVoices = 8;
        private static readonly Journal Log = Logs.Here();

        private readonly List<AudioSource> audioSources = new();

        private int nextAudioSourceIndexToReuse;

        public void Play(SoundAsset sound, float gain = 1f)
        {
            if (!IsServer) return;

            if (sound == null)
            {
                Log.Warn($"Entity {name} was asked to play a sound without an asset set");
                return;
            }

            PlayRpc(sound.NetworkId, sound.PickRandomVariant(), gain);
        }

        [Rpc(SendTo.Everyone)]
        private void PlayRpc(FixedString32Bytes id, byte variant, float gain)
        {
            SoundCatalog soundCatalog = Catalogs.GetCatalog<SoundCatalog>();
            if (soundCatalog == null)
            {
                Log.Warn($"Entity {name} cannot play {id}: the world has no sound catalog");
                return;
            }

            SoundAsset sound = soundCatalog.GetAssetById(id);
            if (sound == null || sound.GetClip(variant) == null) return;

            AudioSource audioSource = TakeAudioSource();
            if (sound.TryConfigure(audioSource, variant, gain)) audioSource.Play();
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
