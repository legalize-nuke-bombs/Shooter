using UnityEngine;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Surface Sounds", fileName = "SurfaceSounds")]
    public class SurfaceSounds : ScriptableObject
    {
        [SerializeField] private SoundSpec fallback;

        [SerializeField] private SurfaceSound[] surfaces;

        public SoundSpec On(PhysicsMaterial surface)
        {
            if (surface == null || surfaces == null) return fallback;

            foreach (SurfaceSound row in surfaces)
                if (row.Surface == surface)
                    return row.Sound != null ? row.Sound : fallback;

            return fallback;
        }
    }
}
