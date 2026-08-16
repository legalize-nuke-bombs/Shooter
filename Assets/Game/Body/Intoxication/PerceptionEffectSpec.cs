using UnityEngine;

namespace Shooter.Game.Body
{
    public abstract class PerceptionEffectSpec : ScriptableObject
    {
        public abstract PerceptionEffect Create();
    }
}
