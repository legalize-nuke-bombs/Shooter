using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Toxin", fileName = "Toxin")]
    public class ToxinSpec : Spec
    {
        [SerializeField] private float halfLife;
        [SerializeField] private PerceptionEffectSpec[] perceptionEffects;

        public float HalfLife => halfLife;

        public PerceptionEffectSpec[] PerceptionEffects => perceptionEffects;
    }
}
