using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShooterEntity))]
    public sealed class ShooterOptimizable : MonoBehaviour
    {
        private ShooterEntity entity;
        private ShooterTransformObserver observer;
        private OptimizerAgent agent;
        private Vector2Int? chunk;
        private int agentCount;

        private void Awake()
        {
            entity = GetComponent<ShooterEntity>();
            observer = GetComponent<ShooterTransformObserver>();
            agent = GetComponent<OptimizerAgent>();
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

        // An enabled agent on the entity itself keeps it awake whatever the count: after a teleport the entity may count
        // its agents before its own agent has moved its count, and finding none would put the agent to sleep with it
        private void UpdateEntityOptimized()
        {
            bool hasEnabledAgent = agent != null && agent.isActiveAndEnabled;
            entity.SetOptimized(agentCount == 0 && !hasEnabledAgent);
        }
    }
}
