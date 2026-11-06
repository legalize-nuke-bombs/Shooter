using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShooterEntity))]
    public sealed class ShooterOptimizable : MonoBehaviour
    {
        private ShooterEntity entity;
        private ShooterTransformObserver observer;
        private Vector2Int? chunk;
        private int agentCount;

        private void Awake()
        {
            entity = GetComponent<ShooterEntity>();
            observer = GetComponent<ShooterTransformObserver>();
            observer.Moved += HandleMoved;
        }

        private void OnDestroy()
        {
            observer.Moved -= HandleMoved;
        }

        public void ChangeAgentCount(int change)
        {
            agentCount += change;
            UpdateEntityOptimized();
        }

        private void HandleMoved()
        {
            Vector2Int movedTo = Chunks.ChunkOf(transform.position);
            if (chunk == movedTo) return;

            chunk = movedTo;
            agentCount = 0;
            foreach (OptimizerAgent anyAgent in Registers.Instance.GetAll<OptimizerAgent>(RegisterInactivePolicy.Include))
            {
                if (anyAgent.KeepsAwake(movedTo)) agentCount++;
            }

            UpdateEntityOptimized();
        }

        private void UpdateEntityOptimized()
        {
            entity.SetOptimized(agentCount == 0);
        }
    }
}
