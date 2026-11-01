using UnityEngine;

namespace Shooter.Infrastructure
{
    [CreateAssetMenu(menuName = "Shooter/Optimizer/Optimization Level", fileName = "OptimizationLevel")]
    public class OptimizationLevelAsset : Asset
    {
        [SerializeField] private bool optimizable;
        [SerializeField] private int chunks;

        public bool Optimizable => optimizable;
        public int Chunks => chunks;
    }
}
