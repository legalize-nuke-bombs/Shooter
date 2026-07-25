using System;
using Unity.Netcode;
using UnityEngine;
using Shooter.Logging;

namespace Shooter.Game.Sounding
{
    [RequireComponent(typeof(AudioSource))]
    public class Speaker : NetworkBehaviour
    {
        [SerializeField] private Voice[] voices;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        public void Play(SoundType sound)
        {
            if (!IsServer) return;

            PlayRpc(sound);
        }

        [Rpc(SendTo.Everyone)]
        private void PlayRpc(SoundType sound)
        {
            AudioClip clip = Clip(sound);
            if (clip == null)
            {
                Log.Warn("Entity {} has no clip for sound {}", name, sound);
                return;
            }

            source.PlayOneShot(clip);
        }

        private AudioClip Clip(SoundType sound)
        {
            foreach (Voice voice in voices)
            {
                if (voice.Sound == sound) return voice.Clip;
            }

            return null;
        }

        [Serializable]
        private struct Voice
        {
            public SoundType Sound;
            public AudioClip Clip;
        }
    }
}
