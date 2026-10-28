using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DefaultExecutionOrder(ExecutionOrder.Infrastructure)]
    public class Chunks : MonoBehaviour
    {
        public const float ChunkSize = 50f;

        private const float CollectIntervalSeconds = 2f;

        public static Chunks Instance { get; private set; }

        public int ChunkCount => chunks.Count;

        private readonly Dictionary<Vector2Int, TMap> chunks = new();
        private float nextCollectTime;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextCollectTime) return;

            nextCollectTime = Time.unscaledTime + CollectIntervalSeconds;
            foreach (TMap chunk in chunks.Values)
            {
                chunk.CollectGarbage();
            }
        }

        public static Vector2Int ChunkOf(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / ChunkSize), Mathf.FloorToInt(position.z / ChunkSize));
        }

        public void AddBehaviour(Vector2Int chunk, MonoBehaviour behaviour)
        {
            if (!chunks.TryGetValue(chunk, out TMap members))
            {
                members = new TMap();
                chunks[chunk] = members;
            }

            members.Add(behaviour);
        }

        public void TryRemoveBehaviour(Vector2Int chunk, MonoBehaviour behaviour)
        {
            if (chunks.TryGetValue(chunk, out TMap members)) members.Remove(behaviour);
        }

        public InRadius<T> FindInRadius<T>(Vector3 center, float radius, ChunkInactivePolicy inactivePolicy) where T : class
        {
            return new InRadius<T>(chunks, center, radius, inactivePolicy);
        }

        public readonly struct InRadius<T> where T : class
        {
            private readonly Dictionary<Vector2Int, TMap> chunks;
            private readonly Vector3 center;
            private readonly float radius;
            private readonly ChunkInactivePolicy inactivePolicy;

            public InRadius(Dictionary<Vector2Int, TMap> chunks, Vector3 center, float radius, ChunkInactivePolicy inactivePolicy)
            {
                this.chunks = chunks;
                this.center = center;
                this.radius = radius;
                this.inactivePolicy = inactivePolicy;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(chunks, center, radius, inactivePolicy);
            }

            public ref struct Enumerator
            {
                private readonly Dictionary<Vector2Int, TMap> chunks;
                private readonly Vector3 center;
                private readonly float radiusSquared;
                private readonly ChunkInactivePolicy inactivePolicy;
                private readonly Vector2Int first;
                private readonly Vector2Int last;
                private Vector2Int chunk;
                private TArray.View<T>.Enumerator members;

                public Enumerator(Dictionary<Vector2Int, TMap> chunks, Vector3 center, float radius, ChunkInactivePolicy inactivePolicy)
                {
                    this.chunks = chunks;
                    this.center = center;
                    this.inactivePolicy = inactivePolicy;
                    radiusSquared = radius * radius;
                    first = ChunkOf(center - new Vector3(radius, 0f, radius));
                    last = ChunkOf(center + new Vector3(radius, 0f, radius));
                    chunk = new Vector2Int(first.x - 1, first.y);
                    members = default;
                }

                public T Current => members.Current;

                public bool MoveNext()
                {
                    while (true)
                    {
                        while (members.MoveNext())
                        {
                            var behaviour = (MonoBehaviour)(object)members.Current;
                            if (inactivePolicy == ChunkInactivePolicy.Exclude && !behaviour.isActiveAndEnabled) continue;
                            if ((behaviour.transform.position - center).sqrMagnitude <= radiusSquared) return true;
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

                        members = found.GetAll<T>().GetEnumerator();
                        return true;
                    }
                }
            }
        }
    }
}
