using Unity.Netcode;
using UnityEngine;
using Shooter.Game;
using Shooter.Logging;

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

        public void Play(SoundType sound)
        {
            if (!IsServer) return;

            PlayRpc(sound);
        }

        [Rpc(SendTo.Everyone)]
        private void PlayRpc(SoundType sound)
        {
            SoundCatalog catalog = Voice;
            if (catalog == null)
            {
                Log.Warn("Entity {} has no sound catalog, neither its own nor the world one", name);
                return;
            }

            AudioClip clip = catalog.Clip(sound);
            if (clip == null) return;

            source.PlayOneShot(clip);
        }
    }
}
