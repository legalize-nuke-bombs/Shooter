using UnityEngine;

namespace Shooter.Game
{
    public abstract class PerceptionEffectSpec : ScriptableObject
    {
        public abstract PerceptionEffect Create();
    }
}
