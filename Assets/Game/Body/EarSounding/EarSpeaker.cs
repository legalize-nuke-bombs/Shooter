using System.Collections.Generic;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;
using Shooter.Game.Core;

namespace Shooter.Game.Body
{
    public class EarSpeaker : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private const int Voices = 4;

        private readonly List<AudioSource> sources = new List<AudioSource>();

        private int next;

        private static EarSoundCatalog Sounds => Catalogs.Of<EarSoundCatalog>();

        public void Play(EarSoundSpec sound)
        {
            if (!IsServer) return;
            if (!NetworkObject.IsPlayerObject) return;

            if (sound == null)
            {
                Log.Warn($"Entity {name} was asked to play an ear sound without a spec set");
                return;
            }

            PlayRpc(sound.Id, sound.Pick());
        }

        public void PlayLocal(EarSoundSpec sound)
        {
            if (sound == null) return;

            Ring(sound, sound.Pick());
        }

        [Rpc(SendTo.Owner)]
        private void PlayRpc(FixedString32Bytes id, byte variant)
        {
            EarSoundCatalog catalog = Sounds;
            if (catalog == null)
            {
                Log.Warn($"Entity {name} cannot play {id} in the ear: the world has no ear sound catalog");
                return;
            }

            EarSoundSpec sound = catalog.Of(id);
            if (sound == null) return;

            Ring(sound, variant);
        }

        private void Ring(EarSoundSpec sound, byte variant)
        {
            AudioClip clip = sound.Clip(variant);
            if (clip == null) return;

            AudioSource source = Free();
            source.clip = clip;
            source.volume = sound.Volume;
            source.pitch = 1f + Random.Range(-sound.PitchVariation, sound.PitchVariation);

            Log.Info($"Entity {name} plays {sound.Id} variant {variant} in the owner's ear at t={Time.time}");

            source.Play();
        }

        private AudioSource Free()
        {
            foreach (AudioSource source in sources)
                if (!source.isPlaying)
                    return source;

            if (sources.Count < Voices) return Add();

            AudioSource taken = sources[next];
            next = (next + 1) % sources.Count;

            return taken;
        }

        private AudioSource Add()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            sources.Add(source);

            return source;
        }
    }
}
