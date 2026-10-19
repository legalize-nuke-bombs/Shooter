using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Toxin", fileName = "Toxin")]
    public class ToxinAsset : Asset
    {
        [SerializeField] private float halfLife;
        [SerializeField] private PerceptionEffectAsset[] perceptionEffects;

        public float HalfLife => halfLife;

        public PerceptionEffectAsset[] PerceptionEffects => perceptionEffects;
    }
}
