using UnityEngine;
using Shooter.Server.Worlds.Entities.Parts.Speaker;

namespace Shooter.Client.Sounds
{
    public class SpeakerView
    {
        private const float MinHearing = 1.5f;
        private const float MaxHearing = 25f;
        private const float PitchSpread = 0.05f;

        private readonly AudioSource source;
        private long lastPlayedId = -1;
        private bool primed;

        public SpeakerView(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = MinHearing;
            source.maxDistance = MaxHearing;
        }

        public void Apply(SpeakerState state)
        {
            if (state == null || state.Recent == null) return;
            if (!primed)
            {
                Prime(state.Recent);
                return;
            }
            foreach (Sound sound in state.Recent)
            {
                if (sound.Id <= lastPlayedId) continue;
                lastPlayedId = sound.Id;
                PlayNow(sound);
            }
        }

        private void Prime(Sound[] sounds)
        {
            foreach (Sound sound in sounds)
                if (sound.Id > lastPlayedId) lastPlayedId = sound.Id;
            primed = true;
        }

        private void PlayNow(Sound sound)
        {
            AudioClip clip = SoundBank.Clip(sound.Type);
            if (clip == null) return;
            source.pitch = 1f + Random.Range(-PitchSpread, PitchSpread);
            source.PlayOneShot(clip);
        }
    }
}
