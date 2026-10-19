using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Surface Sounds", fileName = "SurfaceSounds")]
    public class SurfaceSounds : ScriptableObject
    {
        [SerializeField] private SoundAsset fallback;

        [SerializeField] private SurfaceSound[] surfaces;

        public SoundAsset On(PhysicsMaterial surface)
        {
            if (surface == null || surfaces == null) return fallback;

            foreach (SurfaceSound row in surfaces)
                if (row.Surface == surface)
                    return row.Sound != null ? row.Sound : fallback;

            return fallback;
        }
    }
}
