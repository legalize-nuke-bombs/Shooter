using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.InfrastructureSystemExtension)]
    public class Optimizer : MonoBehaviour
    {
        // An entity wakes within its level's chunks and falls asleep only a chunk farther, so a player stepping back
        // and forth across a chunk border doesn't switch a whole strip of chunks on every step
        private const int SleepMargin = 1;

        private static readonly Journal Log = Logs.Here();

        private Chunks chunks;
        private int updateRadius;
        private HashSet<Vector2Int> chunksSet = new();
        private HashSet<Vector2Int> updatingChunksSet = new();
        private Dictionary<ShooterEntity, Vector2Int> entitiesSet = new();
        private Dictionary<ShooterEntity, Vector2Int> updatingEntitiesSet = new();
        private readonly HashSet<Vector2Int> chunksAround = new();
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
            if (data.Component is ShooterEntity entity) entitiesSet[entity] = data.Chunk;
        }

        private void HandleComponentRemoved(Chunks.OnComponentCallbackData data)
        {
            if (data.Component is ShooterEntity entity) entitiesSet.Remove(entity);
        }

        private void HandleComponentSwitched(Chunks.OnComponentCallbackData data)
        {
            if (data.Component is not OptimizerAgent)
            {
                return;
            }
            Log.Debug($"Optimizer agent is switched at {data.Chunk}");
            chunksSet.Add(data.Chunk);
        }

        // The sets are swapped before the pass: an entity woken by it may place or move others, and they wait for the
        // next frame instead of changing a set under its enumeration
        private void LateUpdate()
        {
            (chunksSet, updatingChunksSet) = (updatingChunksSet, chunksSet);
            (entitiesSet, updatingEntitiesSet) = (updatingEntitiesSet, entitiesSet);

            foreach (Vector2Int chunk in updatingChunksSet)
            {
                for (int x = -updateRadius; x <= updateRadius; x++)
                {
                    for (int z = -updateRadius; z <= updateRadius; z++)
                    {
                        chunksAround.Add(chunk + new Vector2Int(x, z));
                    }
                }
            }

            foreach (Vector2Int chunk in chunksAround)
            {
                int distance = FindDistanceToAgent(chunk);
                foreach (ShooterEntity entity in chunks.GetAll<ShooterEntity>(chunk, ChunkInactivePolicy.Include))
                {
                    UpdateOptimized(entity, distance);
                }
            }

            foreach (KeyValuePair<ShooterEntity, Vector2Int> placed in updatingEntitiesSet)
            {
                UpdateOptimized(placed.Key, FindDistanceToAgent(placed.Value));
            }

            if (updatingChunksSet.Count > 0) Log.Info($"Optimizer went over {chunksAround.Count} chunks around {updatingChunksSet.Count} where agents came or left");
            updatingChunksSet.Clear();
            updatingEntitiesSet.Clear();
            chunksAround.Clear();
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

            int distance = updateRadius + 1;
            for (int ring = 0; ring <= updateRadius; ring++)
            {
                if (!HasAgentOnRing(chunk, ring)) continue;

                distance = ring;
                break;
            }

            distancesToAgent[chunk] = distance;
            return distance;
        }

        private bool HasAgentOnRing(Vector2Int center, int ring)
        {
            for (int x = -ring; x <= ring; x++)
            {
                for (int z = -ring; z <= ring; z++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != ring) continue;

                    if (chunks.CountEnabled(center + new Vector2Int(x, z), typeof(OptimizerAgent)) > 0) return true;
                }
            }

            return false;
        }
    }
}
