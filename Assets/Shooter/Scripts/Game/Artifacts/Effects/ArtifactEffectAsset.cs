using UnityEngine;

namespace Shooter.Game
{
    public abstract class ArtifactEffectAsset : ScriptableObject
    {
        public abstract ArtifactEffect Create();
    }
}
