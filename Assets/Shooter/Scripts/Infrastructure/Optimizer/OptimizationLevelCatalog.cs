using UnityEngine;

namespace Shooter.Infrastructure
{
    [CreateAssetMenu(menuName = "Shooter/Optimizer/Optimization Level Catalog", fileName = "OptimizationLevelCatalog")]
    public class OptimizationLevelCatalog : Catalog<OptimizationLevelAsset>
    {
        public int MaxChunks { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            MaxChunks = 0;

            for (int i = 0; i < Count; i++)
            {
                OptimizationLevelAsset level = GetAssetAt(i);
                if (level.Optimizable) MaxChunks = Mathf.Max(MaxChunks, level.Chunks);
            }
        }
    }
}
