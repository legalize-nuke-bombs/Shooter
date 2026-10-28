using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Registration)]
    public sealed class ShooterEntity : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private WorldPlacement placement;

        private readonly List<MonoBehaviour> components = new();
        private Vector2Int chunk;
        private bool started;

        public WorldPlacement Placement
        {
            get => placement;
            set => placement = value;
        }

        private void Awake()
        {
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (component != null) Register(component);
            }
        }

        private void Start()
        {
            started = true;
            if (placement == WorldPlacement.None) return;

            chunk = Chunks.ChunkOf(transform.position);
            foreach (MonoBehaviour component in components)
            {
                Chunks.Instance.AddComponent(chunk, component);
            }

            if (placement == WorldPlacement.Movable) AddComponent<TransformObserver>().Moved += HandleMoved;
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
                Chunks.Instance.TryRemoveComponent(chunk, component);
            }
        }

        public T AddComponent<T>() where T : MonoBehaviour
        {
            T component = gameObject.AddComponent<T>();
            Register(component);
            if (isActiveAndEnabled) Registers.Instance.ActivateComponent(component);
            if (started && placement != WorldPlacement.None) Chunks.Instance.AddComponent(chunk, component);
            return component;
        }

        private void HandleMoved()
        {
            Vector2Int movedTo = Chunks.ChunkOf(transform.position);
            if (movedTo == chunk) return;

            foreach (MonoBehaviour component in components)
            {
                Chunks.Instance.TryRemoveComponent(chunk, component);
                Chunks.Instance.AddComponent(movedTo, component);
            }

            Log.Debug($"Entity {name} moved from chunk {chunk} to chunk {movedTo}");
            chunk = movedTo;
        }

        private void Register(MonoBehaviour component)
        {
            components.Add(component);
            Registers.Instance.RegisterComponent(component);
        }
    }
}
