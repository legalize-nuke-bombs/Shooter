using System.Collections.Generic;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body.Sounding
{
    public class Speaker : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private const int Voices = 8;

        [SerializeField] private SoundCatalog voice;

        private readonly List<AudioSource> sources = new List<AudioSource>();

        private int next;

        private SoundCatalog Voice => voice != null
            ? voice
            : Environment.Current == null ? null : Environment.Current.Sounds;

        public void Play(SoundSpec sound)
        {
            if (!IsServer || sound == null) return;

            PlayRpc(sound.Id, sound.Pick());
        }

        [Rpc(SendTo.Everyone)]
        private void PlayRpc(FixedString32Bytes id, byte variant)
        {
            SoundCatalog catalog = Voice;
            if (catalog == null)
            {
                Log.Warn("Entity {} has no sound catalog, neither its own nor the world one", name);
                return;
            }

            SoundSpec sound = catalog.Of(id);
            if (sound == null) return;

            AudioClip clip = sound.Clip(variant);
            if (clip == null) return;

            AudioSource source = Free();
            source.clip = clip;
            source.volume = sound.Volume;
            source.rolloffMode = sound.Rolloff;
            source.minDistance = sound.MinDistance;
            source.maxDistance = sound.MaxDistance;
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
            source.spatialBlend = 1f;
            sources.Add(source);

            return source;
        }
    }
}
