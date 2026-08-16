using UnityEngine;

namespace Shooter.Game.Body.Perception
{
    public abstract class PerceptionEffectSpec : ScriptableObject
    {
        public abstract PerceptionEffect Create(IPerceiver perceiver);
    }
}
