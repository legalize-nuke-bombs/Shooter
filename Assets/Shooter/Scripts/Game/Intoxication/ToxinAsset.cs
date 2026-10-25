using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Toxin", fileName = "Toxin")]
    public class ToxinAsset : Asset
    {
        [SerializeField] private float halfLifeHours;
        [SerializeField] private PerceptionEffectAsset[] perceptionEffects;

        public float HalfLifeHours => halfLifeHours;

        public PerceptionEffectAsset[] PerceptionEffects => perceptionEffects;
    }
}
