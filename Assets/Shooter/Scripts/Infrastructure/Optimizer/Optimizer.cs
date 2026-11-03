using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.InfrastructureSystemExtension)]
    public class Optimizer : MonoBehaviour
    {
        // An entity wakes within its level's chunks and falls asleep only beyond them plus the margin, so a player
        // stepping back and forth across a chunk border doesn't switch a strip of chunks on every step
        private const int SleepMargin = 1;

        private static readonly Journal Log = Logs.Here();

        private Chunks chunks;
        private int updateRadius;
        private readonly HashSet<Vector2Int> switchedChunks = new();
        private readonly Dictionary<ShooterEntity, Vector2Int> placedEntities = new();
        private readonly HashSet<Vector2Int> chunksToUpdate = new();
        private readonly List<KeyValuePair<ShooterEntity, Vector2Int>> entitiesToUpdate = new();
        private readonly Dictionary<Vector2Int, int> distancesToAgent = new();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            updateRadius = Catalogs.GetCatalog<OptimizationLevelCatalog>().MaxChunks + SleepMargin;
        }

        private void OnEnable()
        {
            chunks = Registers.Instance.Get<Chunks>();
            chunks.OnComponentAdded += HandleComponentAdded;
            chunks.OnComponentRemoved += HandleComponentRemoved;
            chunks.OnComponentEnabled += HandleComponentSwitched;
            chunks.OnComponentDisabled += HandleComponentSwitched;
        }

        private void OnDisable()
        {
            chunks.OnComponentDisabled -= HandleComponentSwitched;
            chunks.OnComponentEnabled -= HandleComponentSwitched;
            chunks.OnComponentRemoved -= HandleComponentRemoved;
            chunks.OnComponentAdded -= HandleComponentAdded;
        }

        private void HandleComponentAdded(Chunks.OnComponentCallbackData data)
        {
            if (data.Component is ShooterEntity entity) placedEntities[entity] = data.Chunk;
        }

        private void HandleComponentRemoved(Chunks.OnComponentCallbackData data)
        {
            if (data.Component is ShooterEntity entity) placedEntities.Remove(entity);
        }

        private void HandleComponentSwitched(Chunks.OnComponentCallbackData data)
        {
            if (data.Component is not OptimizerAgent)
            {
                return;
            }
            Log.Debug($"Optimizer agent is switched at {data.Chunk}");
            switchedChunks.Add(data.Chunk);
        }

        // The pass takes what was collected before it starts: an entity it wakes may place or move others, and they
        // wait for the next frame instead of changing a collection under its enumeration
        private void LateUpdate()
        {
            foreach (Vector2Int switchedChunk in switchedChunks)
            {
                for (int x = -updateRadius; x <= updateRadius; x++)
                {
                    for (int z = -updateRadius; z <= updateRadius; z++)
                    {
                        chunksToUpdate.Add(switchedChunk + new Vector2Int(x, z));
                    }
                }
            }
            entitiesToUpdate.AddRange(placedEntities);
            if (switchedChunks.Count > 0) Log.Info($"Optimizer goes over {chunksToUpdate.Count} chunks around {switchedChunks.Count} where agents came or left");
            switchedChunks.Clear();
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

        private static void UpdateOptimized(ShooterEntity entity, int distanceToAgent)
        {
            OptimizationLevelAsset level = entity.OptimizationLevel;
            if (level == null || !level.Optimizable) return;

            if (distanceToAgent <= level.Chunks) entity.SetOptimized(false);
            else if (distanceToAgent > level.Chunks + SleepMargin) entity.SetOptimized(true);
        }

        private int FindDistanceToAgent(Vector2Int chunk)
        {
            if (distancesToAgent.TryGetValue(chunk, out int known)) return known;

            int distance = int.MaxValue;
            for (int x = -updateRadius; x <= updateRadius; x++)
            {
                for (int z = -updateRadius; z <= updateRadius; z++)
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
