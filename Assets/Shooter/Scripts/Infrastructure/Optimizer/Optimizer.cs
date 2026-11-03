using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.InfrastructureSystemExtension)]
    public class Optimizer : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Chunks chunks;
        private readonly HashSet<Vector2Int> chunksSet = new();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            chunks = Registers.Instance.Get<Chunks>();
            chunks.OnComponentEnabled += HandleComponentSwitched;
            chunks.OnComponentDisabled += HandleComponentSwitched;
        }

        private void OnDisable()
        {
            chunks.OnComponentDisabled -= HandleComponentSwitched;
            chunks.OnComponentEnabled -= HandleComponentSwitched;
        }

        private void HandleComponentSwitched(Chunks.OnComponentSwitchedCallbackData data)
        {
            if (data.Component is not OptimizerAgent)
            {
                return;
            }
            Log.Debug($"Optimizer agent is switched at {data.Chunk}");
            chunksSet.Add(data.Chunk);
        }

        private void LateUpdate()
        {
            foreach (Vector2Int chunk in chunksSet)
            {
                SetChunkOptimized(chunk, chunks.CountEnabled(chunk, typeof(OptimizerAgent)) > 0);
            }
            chunksSet.Clear();
        }

        private void SetChunkOptimized(Vector2Int chunk, bool value)
        {
            Log.Info($"Entity {name} is {(value ? "optimizing" : "deoptimizing")} chunk {chunk}");
        }
    }
}

