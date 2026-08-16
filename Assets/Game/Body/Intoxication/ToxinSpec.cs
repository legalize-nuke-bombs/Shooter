using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Toxin", fileName = "Toxin")]
    public class ToxinSpec : Spec
    {
        [SerializeField] private float halfLife;
        [SerializeField] private PerceptionEffect[] perceptionEffects;

        public float HalfLife => halfLife;

        public PerceptionEffect[] PerceptionEffects => perceptionEffects;
    }
}
