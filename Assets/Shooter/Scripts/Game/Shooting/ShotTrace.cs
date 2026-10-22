using UnityEngine;

namespace Shooter.Game
{
    public readonly struct ShotTrace
    {
        public ShotTrace(Vector3 origin, Vector3 end, SoundAsset impactSound, byte impactSoundVariant)
        {
            Origin = origin;
            End = end;
            ImpactSound = impactSound;
            ImpactSoundVariant = impactSoundVariant;
        }

        public Vector3 Origin { get; }

        public Vector3 End { get; }

        public SoundAsset ImpactSound { get; }

        public byte ImpactSoundVariant { get; }
    }
}
