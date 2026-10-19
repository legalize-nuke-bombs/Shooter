using System;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public struct SurfaceSound
    {
        [SerializeField] private PhysicsMaterial surface;
        [SerializeField] private SoundAsset sound;

        public PhysicsMaterial Surface => surface;
        public SoundAsset Sound => sound;
    }
}
