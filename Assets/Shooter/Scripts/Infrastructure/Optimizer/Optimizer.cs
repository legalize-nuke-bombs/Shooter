using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.InfrastructureSystemExtension)]
    public class Optimizer : MonoBehaviour
    {
        private const int WakeRadius = 2;
        // An entity falls asleep only beyond the wake radius plus the margin, so a player stepping back and forth across
        // a chunk border doesn't switch a strip of chunks on every step
        private const int SleepMargin = 1;
        private const int UpdateRadius = WakeRadius + SleepMargin;

        private static readonly Journal Log = Logs.Here();

        private Chunks chunks;
        private readonly HashSet<Vector2Int> arrivedChunks = new();
        private readonly HashSet<Vector2Int> leftChunks = new();
        private readonly Dictionary<ShooterEntity, Vector2Int> placedEntities = new();
        private readonly HashSet<Vector2Int> chunksToWake = new();
        private readonly HashSet<Vector2Int> chunksToUpdate = new();
        private readonly List<KeyValuePair<ShooterEntity, Vector2Int>> entitiesToUpdate = new();
        private readonly Dictionary<Vector2Int, int> distancesToAgent = new();
        private bool isWaking;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            chunks = Registers.Instance.Get<Chunks>();
            chunks.OnComponentAdded += HandleComponentAdded;
            chunks.OnComponentRemoved += HandleComponentRemoved;
            chunks.OnComponentEnabled += HandleComponentEnabled;
            chunks.OnComponentDisabled += HandleComponentDisabled;
        }

        private void OnDisable()
        {
            chunks.OnComponentDisabled -= HandleComponentDisabled;
            chunks.OnComponentEnabled -= HandleComponentEnabled;
            chunks.OnComponentRemoved -= HandleComponentRemoved;
            chunks.OnComponentAdded -= HandleComponentAdded;
        }

        private void HandleComponentAdded(Chunks.OnComponentCallbackData data)
        {
            if (data.Component is ShooterEntity entity && entity.Optimizable) placedEntities[entity] = data.Chunk;
        }

        // A move puts an entity into the new chunk before it takes it out of the old one
        private void HandleComponentRemoved(Chunks.OnComponentCallbackData data)
        {
            if (data.Component is not ShooterEntity entity) return;

            if (placedEntities.TryGetValue(entity, out Vector2Int placedAt) && placedAt == data.Chunk) placedEntities.Remove(entity);
        }

        // Waking happens right in the callback: a world asleep around an agent for even a frame is wrong for whoever uses
        // it. Putting to sleep and placed entities wait for LateUpdate, when every move of the frame is done: in the middle
        // of one, an entity can stand in its new chunk while its own agent is still in the old one
        private void HandleComponentEnabled(Chunks.OnComponentCallbackData data)
        {
            if (data.Component is not OptimizerAgent) return;

            Log.Debug($"Optimizer agent came to {data.Chunk}");
            arrivedChunks.Add(data.Chunk);
            WakeAroundArrived();
        }

        private void HandleComponentDisabled(Chunks.OnComponentCallbackData data)
        {
            if (data.Component is not OptimizerAgent) return;

            Log.Debug($"Optimizer agent left {data.Chunk}");
            leftChunks.Add(data.Chunk);
        }

        // What waking switches may call back here, and that is taken by the next round of the same call
        private void WakeAroundArrived()
        {
            if (isWaking) return;

            isWaking = true;
            try
            {
                while (arrivedChunks.Count > 0)
                {
                    CollectChunksAround(arrivedChunks, WakeRadius, chunksToWake);
                    distancesToAgent.Clear();
                    foreach (Vector2Int chunk in chunksToWake)
                    {
                        if (FindDistanceToAgent(chunk) > WakeRadius) continue;

                        foreach (ShooterEntity entity in chunks.GetAll<ShooterEntity>(chunk, ChunkInactivePolicy.Include))
                        {
                            if (entity.Optimizable) entity.SetOptimized(false);
                        }
                    }

                    chunksToWake.Clear();
                    distancesToAgent.Clear();
                }
            }
            finally
            {
                isWaking = false;
            }
        }

        private void LateUpdate()
        {
            CollectChunksAround(leftChunks, UpdateRadius, chunksToUpdate);
            entitiesToUpdate.AddRange(placedEntities);
            placedEntities.Clear();

            foreach (Vector2Int chunk in chunksToUpdate)
            {
                int distance = FindDistanceToAgent(chunk);
                foreach (ShooterEntity entity in chunks.GetAll<ShooterEntity>(chunk, ChunkInactivePolicy.Include))
                {
                    UpdateOptimized(entity, distance);
                }
            }

            foreach (KeyValuePair<ShooterEntity, Vector2Int> placed in entitiesToUpdate)
            {
                UpdateOptimized(placed.Key, FindDistanceToAgent(placed.Value));
            }

            chunksToUpdate.Clear();
            entitiesToUpdate.Clear();
            distancesToAgent.Clear();
        }

        private static void CollectChunksAround(HashSet<Vector2Int> switched, int radius, HashSet<Vector2Int> collected)
        {
            foreach (Vector2Int switchedChunk in switched)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        collected.Add(switchedChunk + new Vector2Int(x, z));
                    }
                }
            }

            if (switched.Count > 0) Log.Info($"Optimizer goes over {collected.Count} chunks around {switched.Count} where agents came or left");
            switched.Clear();
        }

        private static void UpdateOptimized(ShooterEntity entity, int distanceToAgent)
        {
            if (!entity.Optimizable) return;

            if (distanceToAgent <= WakeRadius) entity.SetOptimized(false);
            else if (distanceToAgent > WakeRadius + SleepMargin) entity.SetOptimized(true);
        }

        private int FindDistanceToAgent(Vector2Int chunk)
        {
            if (distancesToAgent.TryGetValue(chunk, out int known)) return known;

            int distance = int.MaxValue;
            for (int x = -UpdateRadius; x <= UpdateRadius; x++)
            {
                for (int z = -UpdateRadius; z <= UpdateRadius; z++)
                {
                    if (chunks.CountEnabled(chunk + new Vector2Int(x, z), typeof(OptimizerAgent)) == 0) continue;

                    distance = Mathf.Min(distance, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)));
                }
            }

            distancesToAgent[chunk] = distance;
            return distance;
        }
    }
}
