using System;
using UnityEngine;

namespace Shooter.Game.Body
{
    [Serializable]
    public struct SurfaceSound
    {
        [SerializeField] private PhysicsMaterial surface;
        [SerializeField] private SoundSpec sound;

        public PhysicsMaterial Surface => surface;
        public SoundSpec Sound => sound;
    }
}
