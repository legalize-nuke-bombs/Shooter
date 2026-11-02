using UnityEngine;

namespace Shooter.Infrastructure
{
    [CreateAssetMenu(menuName = "Shooter/Optimizer/Optimization Level", fileName = "OptimizationLevel")]
    public class OptimizationLevelAsset : Asset
    {
        [SerializeField] private bool optimizable = true;
        [SerializeField] private int chunks = 1;

        public bool Optimizable => optimizable;
        public int Chunks => chunks;
    }
}
