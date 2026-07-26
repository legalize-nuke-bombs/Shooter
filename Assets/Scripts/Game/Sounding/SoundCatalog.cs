using System;
using UnityEngine;
using Shooter.Logging;

namespace Shooter.Game.Sounding
{
    [CreateAssetMenu(menuName = "Shooter/Sound Catalog", fileName = "SoundCatalog")]
    public class SoundCatalog : ScriptableObject
    {
        [SerializeField] private Voice[] voices;

        public AudioClip Clip(SoundType sound)
        {
            foreach (Voice voice in voices)
            {
                if (voice.Clip != null && voice.Sound == sound) return voice.Clip;
            }

            Log.Warn("Sound catalog {} has no clip for {}", name, sound);
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
