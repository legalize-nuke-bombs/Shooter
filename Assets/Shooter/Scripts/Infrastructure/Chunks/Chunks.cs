using System;
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

        public static Chunks Instance { get; private set; }

        public int ChunkCount => components.Count;

        private readonly Dictionary<Vector2Int, TMap> components = new();
        private readonly Dictionary<Vector2Int, TMap> activeComponents = new();
        private float nextCollectTime;

        public struct OnComponentSwitchedCallbackData
        {
            public Vector2Int Chunk;
            public Behaviour Component;
        }
        public Action<OnComponentSwitchedCallbackData> OnComponentEnabled;
        public Action<OnComponentSwitchedCallbackData> OnComponentDisabled;


        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextCollectTime) return;

            nextCollectTime = Time.unscaledTime + CollectIntervalSeconds;
            foreach (TMap chunk in components.Values)
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

        public void AddComponent(Vector2Int chunk, Behaviour component)
        {
            FindOrCreate(components, chunk).TryAdd(component);
        }

        public void TryRemoveComponent(Vector2Int chunk, Behaviour component)
        {
            if (components.TryGetValue(chunk, out TMap members)) members.Remove(component);
            TryDeactivateComponent(chunk, component);
        }

        public void ActivateComponent(Vector2Int chunk, Behaviour component)
        {
            if (FindOrCreate(activeComponents, chunk).TryAdd(component))
            {
                OnComponentEnabled?.Invoke(new OnComponentSwitchedCallbackData()
                {
                    Chunk = chunk,
                    Component = component
                });
            }
        }

        public void TryDeactivateComponent(Vector2Int chunk, Behaviour component)
        {
            if (activeComponents.TryGetValue(chunk, out TMap members)) members.Remove(component);
        }

        public View<T> FindInRadius<T>(Vector3 center, float radius, ChunkInactivePolicy inactivePolicy) where T : class
        {
            return new View<T>(inactivePolicy == ChunkInactivePolicy.Include ? components : activeComponents, center, radius);
        }

        private static TMap FindOrCreate(Dictionary<Vector2Int, TMap> chunks, Vector2Int chunk)
        {
            if (chunks.TryGetValue(chunk, out TMap members)) return members;

            members = new TMap();
            chunks[chunk] = members;
            return members;
        }

        public readonly struct View<T> where T : class
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
                private TArray.View<T>.Enumerator members;

                public Enumerator(Dictionary<Vector2Int, TMap> chunks, Vector3 center, float radius)
                {
                    this.chunks = chunks;
                    this.center = center;
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
                            var component = (Behaviour)(object)members.Current;
                            if ((component.transform.position - center).sqrMagnitude <= radiusSquared) return true;
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
