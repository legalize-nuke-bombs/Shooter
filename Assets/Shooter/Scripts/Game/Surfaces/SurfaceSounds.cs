using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Surface Sounds", fileName = "SurfaceSounds")]
    public class SurfaceSounds : ScriptableObject
    {
        [SerializeField] private SoundAsset fallback;

        [SerializeField] private SurfaceSound[] surfaces;

        public SoundAsset GetSoundOn(PhysicsMaterial surface)
        {
            if (surface == null || surfaces == null) return fallback;

            foreach (SurfaceSound surfaceSound in surfaces)
            {
                if (surfaceSound.Surface == surface) return surfaceSound.Sound != null ? surfaceSound.Sound : fallback;
            }

            return fallback;
        }
    }
}
