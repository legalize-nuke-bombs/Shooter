using UnityEngine;

namespace Shooter.Game.Body
{
    public abstract class PerceptionEffect : ScriptableObject
    {
        public abstract PerceptionEffectInstance Begin();
    }

    public abstract class PerceptionEffectInstance
    {
        public abstract void Tick(float strength);

        public abstract void End();
    }
}
