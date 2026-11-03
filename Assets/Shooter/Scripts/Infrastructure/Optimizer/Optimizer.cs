using System;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.InfrastructureSystemExtension)]
    public class Optimizer : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Chunks chunks;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            chunks = Registers.Instance.Get<Chunks>();
            chunks.OnComponentEnabled += HandleComponentEnabled;
            chunks.OnComponentDisabled += HandleComponentDisabled;
        }

        private void OnDisable()
        {
            chunks.OnComponentDisabled -= HandleComponentDisabled;
            chunks.OnComponentEnabled -= HandleComponentEnabled;
        }

        private void HandleComponentEnabled(Chunks.OnComponentSwitchedCallbackData data)
        {
            if (data.Component is OptimizerAgent)
            {
                Log.Warn($"Optimizer agent is enabled at {data.Chunk}");
            }
        }

        private void HandleComponentDisabled(Chunks.OnComponentSwitchedCallbackData data)
        {
            if (data.Component is OptimizerAgent)
            {
                Log.Warn($"Optimizer agent is disabled at {data.Chunk}");
            }
        }
    }
}

