using UnityEngine;

namespace Shooter.Game
{
    public abstract class ArtifactEffect
    {
        public abstract void Enable(GameObject self);
        public abstract void Disable();

        public abstract void Update(float dt);
    }
}
