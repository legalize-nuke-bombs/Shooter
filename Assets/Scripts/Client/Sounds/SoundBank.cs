using System.Collections.Generic;
using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Speaker;

namespace Shooter.Client.Sounds
{
    public static class SoundBank
    {
        private static readonly Dictionary<SoundType, AudioClip> clips = new Dictionary<SoundType, AudioClip>();

        public static AudioClip Clip(SoundType soundType)
        {
            if (clips.TryGetValue(soundType, out AudioClip cached)) return cached;
            var loaded = Resources.Load<AudioClip>("Sounds/" + soundType);
            if (loaded == null) Log.Warn("Sound {} has no clip at Resources/Sounds/{}", soundType, soundType);
            clips[soundType] = loaded;
            return loaded;
        }
    }
}
