using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.InfrastructureSystemCore)]
    public class Chunks : MonoBehaviour
    {
        public const float ChunkSize = 50f;

        private const float CollectIntervalSeconds = 2f;

        public int ChunkCount => members.Count;

        private readonly Dictionary<Vector2Int, TMap> members = new();
        private readonly Dictionary<Vector2Int, TMap> activeComponents = new();
        private float nextCollectTime;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextCollectTime) return;

            nextCollectTime = Time.unscaledTime + CollectIntervalSeconds;
            foreach (TMap chunk in members.Values)
            {
                chunk.CollectGarbage();
            }

            foreach (TMap chunk in activeComponents.Values)
            {
                chunk.CollectGarbage();
            }
        }

        public static Vector2Int ChunkOf(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / ChunkSize), Mathf.FloorToInt(position.z / ChunkSize));
        }

        public void Add(Vector2Int chunk, object member)
        {
            FindOrCreate(members, chunk).TryAdd(member);
        }

        public void Remove(Vector2Int chunk, object member)
        {
            if (activeComponents.TryGetValue(chunk, out TMap activeInChunk)) activeInChunk.TryRemove(member);
            if (members.TryGetValue(chunk, out TMap inChunk)) inChunk.TryRemove(member);
        }

        public void ActivateComponent(Vector2Int chunk, Behaviour component)
        {
            FindOrCreate(activeComponents, chunk).TryAdd(component);
        }

        public void TryDeactivateComponent(Vector2Int chunk, Behaviour component)
        {
            if (activeComponents.TryGetValue(chunk, out TMap activeInChunk)) activeInChunk.TryRemove(component);
        }

        public TArray.View<T> GetAll<T>(Vector2Int chunk, ChunkInactivePolicy inactivePolicy) where T : class
        {
            Dictionary<Vector2Int, TMap> chunks = inactivePolicy == ChunkInactivePolicy.Include ? members : activeComponents;
            return chunks.TryGetValue(chunk, out TMap inChunk) ? inChunk.GetAll<T>() : default;
        }

        public View<T> FindInRadius<T>(Vector3 center, float radius, ChunkInactivePolicy inactivePolicy) where T : Component
        {
            return new View<T>(inactivePolicy == ChunkInactivePolicy.Include ? members : activeComponents, center, radius);
        }

        private static TMap FindOrCreate(Dictionary<Vector2Int, TMap> chunks, Vector2Int chunk)
        {
            if (chunks.TryGetValue(chunk, out TMap inChunk)) return inChunk;

            inChunk = new TMap();
            chunks[chunk] = inChunk;
            return inChunk;
        }

        public readonly struct View<T> where T : Component
        {
            private readonly Dictionary<Vector2Int, TMap> chunks;
            private readonly Vector3 center;
            private readonly float radius;

            public View(Dictionary<Vector2Int, TMap> chunks, Vector3 center, float radius)
            {
                this.chunks = chunks;
                this.center = center;
                this.radius = radius;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(chunks, center, radius);
            }

            public ref struct Enumerator
            {
                private readonly Dictionary<Vector2Int, TMap> chunks;
                private readonly Vector3 center;
                private readonly float radiusSquared;
                private readonly Vector2Int first;
                private readonly Vector2Int last;
                private Vector2Int chunk;
                private TArray.View<T>.Enumerator inChunk;

                public Enumerator(Dictionary<Vector2Int, TMap> chunks, Vector3 center, float radius)
                {
                    this.chunks = chunks;
                    this.center = center;
                    radiusSquared = radius * radius;
                    first = ChunkOf(center - new Vector3(radius, 0f, radius));
                    last = ChunkOf(center + new Vector3(radius, 0f, radius));
                    chunk = new Vector2Int(first.x - 1, first.y);
                    inChunk = default;
                }

                public T Current => inChunk.Current;

                public bool MoveNext()
                {
                    while (true)
                    {
                        while (inChunk.MoveNext())
                        {
                            if ((inChunk.Current.transform.position - center).sqrMagnitude <= radiusSquared) return true;
                        }

                        if (!TryEnterNextChunk()) return false;
                    }
                }

                private bool TryEnterNextChunk()
                {
                    while (true)
                    {
                        chunk.x++;
                        if (chunk.x > last.x)
                        {
                            chunk.x = first.x;
                            chunk.y++;
                        }

                        if (chunk.y > last.y) return false;
                        if (!chunks.TryGetValue(chunk, out TMap found)) continue;

                        inChunk = found.GetAll<T>().GetEnumerator();
                        return true;
                    }
                }
            }
        }
    }
}
