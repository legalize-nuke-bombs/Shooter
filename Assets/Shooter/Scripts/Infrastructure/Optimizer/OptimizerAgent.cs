using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShooterEntity))]
    public sealed class OptimizerAgent : MonoBehaviour, IWantsEnabledHandler
    {
        private const int Radius = 2;

        private ShooterTransformObserver observer;
        private Chunks chunks;
        private Vector2Int? chunk;
        private Vector2Int? countedAt;
        private bool wantsEnabled;

        public bool KeepsAwake(Vector2Int optimizableChunk)
        {
            return countedAt != null && DistanceBetween(countedAt.Value, optimizableChunk) <= Radius;
        }

        private void Awake()
        {
            observer = GetComponent<ShooterTransformObserver>();
            chunks = Registers.Instance.Get<Chunks>();
            wantsEnabled = enabled;
            observer.Moved += HandleMoved;
        }

        private void OnDestroy()
        {
            observer.Moved -= HandleMoved;
            CountAt(null);
        }

        public void OnWantsEnabled()
        {
            wantsEnabled = true;
            CountAt(chunk);
        }

        public void OnWantsDisabled()
        {
            wantsEnabled = false;
            CountAt(null);
        }

        private void HandleMoved()
        {
            chunk = Chunks.ChunkOf(transform.position);
            if (wantsEnabled) CountAt(chunk);
        }

        // The new square is counted before the old one is let go, so whatever stands in both never drops to no agents
        private void CountAt(Vector2Int? newChunk)
        {
            if (countedAt == newChunk) return;

            Vector2Int? oldChunk = countedAt;
            countedAt = newChunk;
            if (newChunk != null) ChangeAgentCountAround(newChunk.Value, oldChunk, 1);
            if (oldChunk != null) ChangeAgentCountAround(oldChunk.Value, newChunk, -1);
        }

        private void ChangeAgentCountAround(Vector2Int center, Vector2Int? alreadyCountedCenter, int change)
        {
            for (int x = -Radius; x <= Radius; x++)
            {
                for (int z = -Radius; z <= Radius; z++)
                {
                    Vector2Int around = center + new Vector2Int(x, z);
                    if (alreadyCountedCenter != null && DistanceBetween(around, alreadyCountedCenter.Value) <= Radius) continue;

                    foreach (ShooterOptimizable found in chunks.GetAll<ShooterOptimizable>(around, ChunkInactivePolicy.Include))
                    {
                        found.ChangeAgentCount(change);
                    }
                }
            }
        }

        private static int DistanceBetween(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }
    }
}
