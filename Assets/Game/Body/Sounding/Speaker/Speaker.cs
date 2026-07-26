using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body.Sounding
{
    [RequireComponent(typeof(AudioSource))]
    public class Speaker : NetworkBehaviour
    {
        [SerializeField] private SoundCatalog voice;

        private AudioSource source;

        private SoundCatalog Voice => voice != null
            ? voice
            : Environment.Current == null ? null : Environment.Current.Sounds;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
        }

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

            source.PlayOneShot(clip, sound.Volume);
        }
    }
}
