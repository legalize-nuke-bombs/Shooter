using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShooterTransformObserver))]
    [DefaultExecutionOrder(ExecutionOrder.Registration)]
    public sealed class ShooterEntity : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private readonly List<MonoBehaviour> components = new();
        private Vector2Int? chunk;

        private void Awake()
        {
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (component != null) Register(component);
            }

            ShooterTransformObserver observer = GetComponent<ShooterTransformObserver>();
            observer.Placed += HandlePlaced;
            observer.Moved += HandleMoved;
            observer.Removed += HandleRemoved;
        }

        private void OnEnable()
        {
            foreach (MonoBehaviour component in components)
            {
                Registers.Instance.ActivateComponent(component);
            }
        }

        private void OnDisable()
        {
            foreach (MonoBehaviour component in components)
            {
                Registers.Instance.DeactivateComponent(component);
            }
        }

        private void OnDestroy()
        {
            foreach (MonoBehaviour component in components)
            {
                Registers.Instance.UnregisterComponent(component);
            }
        }

        public T AddComponent<T>() where T : MonoBehaviour
        {
            T component = gameObject.AddComponent<T>();
            Register(component);
            if (isActiveAndEnabled) Registers.Instance.ActivateComponent(component);
            if (chunk != null) Chunks.Instance.AddComponent(chunk.Value, component);
            return component;
        }

        private void HandlePlaced()
        {
            Vector2Int cell = Chunks.ChunkOf(transform.position);
            foreach (MonoBehaviour component in components)
            {
                Chunks.Instance.AddComponent(cell, component);
            }

            chunk = cell;
        }

        private void HandleMoved()
        {
            if (chunk == null) return;

            Vector2Int movedFrom = chunk.Value;
            Vector2Int movedTo = Chunks.ChunkOf(transform.position);
            if (movedTo == movedFrom) return;

            foreach (MonoBehaviour component in components)
            {
                Chunks.Instance.TryRemoveComponent(movedFrom, component);
                Chunks.Instance.AddComponent(movedTo, component);
            }

            Log.Debug($"Entity {name} moved from chunk {movedFrom} to chunk {movedTo}");
            chunk = movedTo;
        }

        private void HandleRemoved()
        {
            if (chunk == null) return;

            foreach (MonoBehaviour component in components)
            {
                Chunks.Instance.TryRemoveComponent(chunk.Value, component);
            }

            chunk = null;
        }

        private void Register(MonoBehaviour component)
        {
            components.Add(component);
            Registers.Instance.RegisterComponent(component);
        }
    }
}
