using System;
using System.Collections.Generic;
using UnityEngine;
using Shooter.Logging;

namespace Shooter.Game.Sounding
{
    [CreateAssetMenu(menuName = "Shooter/Sound Catalog", fileName = "SoundCatalog")]
    public class SoundCatalog : ScriptableObject
    {
        [SerializeField] private Voice[] voices;

        private readonly HashSet<SoundType> unknown = new HashSet<SoundType>();

        public AudioClip Clip(SoundType sound)
        {
            foreach (Voice voice in voices)
            {
                if (voice.Clip != null && voice.Sound == sound) return voice.Clip;
            }

            if (unknown.Add(sound)) Log.Warn("Sound catalog {} has no clip for {}", name, sound);

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
