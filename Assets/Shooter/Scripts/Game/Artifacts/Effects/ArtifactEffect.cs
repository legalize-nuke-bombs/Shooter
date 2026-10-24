using UnityEngine;

namespace Shooter.Game
{
    public abstract class ArtifactEffect : ScriptableObject
    {
        public abstract void Update(GameObject user, float dt);
    }
}
