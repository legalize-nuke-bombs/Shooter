using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Registration)]
    public sealed class ShooterObject : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private WorldPlacement placement;

        private readonly List<MonoBehaviour> behaviours = new();
        private Vector2Int chunk;
        private bool placed;

        public WorldPlacement Placement
        {
            get => placement;
            set => placement = value;
        }

        private void Awake()
        {
            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour != null) Register(behaviour);
            }
        }

        private void Start()
        {
            if (placement == WorldPlacement.None) return;

            chunk = Chunks.ChunkOf(transform.position);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                Chunks.Instance.AddBehaviour(chunk, behaviour);
            }

            placed = true;
            if (placement == WorldPlacement.Movable) AddComponent<TransformObserver>().Moved += HandleMoved;
        }

        private void OnEnable()
        {
            foreach (MonoBehaviour behaviour in behaviours)
            {
                Registers.Instance.ActivateBehaviour(behaviour);
            }
        }

        private void OnDisable()
        {
            foreach (MonoBehaviour behaviour in behaviours)
            {
                Registers.Instance.DeactivateBehaviour(behaviour);
            }
        }

        private void OnDestroy()
        {
            foreach (MonoBehaviour behaviour in behaviours)
            {
                Registers.Instance.UnregisterBehaviour(behaviour);
                if (placed) Chunks.Instance.RemoveBehaviour(chunk, behaviour);
            }
        }

        public T AddComponent<T>() where T : MonoBehaviour
        {
            T behaviour = gameObject.AddComponent<T>();
            Register(behaviour);
            if (isActiveAndEnabled) Registers.Instance.ActivateBehaviour(behaviour);
            if (placed) Chunks.Instance.AddBehaviour(chunk, behaviour);
            return behaviour;
        }

        private void HandleMoved()
        {
            Vector2Int movedTo = Chunks.ChunkOf(transform.position);
            if (movedTo == chunk) return;

            foreach (MonoBehaviour behaviour in behaviours)
            {
                Chunks.Instance.RemoveBehaviour(chunk, behaviour);
                Chunks.Instance.AddBehaviour(movedTo, behaviour);
            }

            Log.Debug($"{name} moved from chunk {chunk} to chunk {movedTo}");
            chunk = movedTo;
        }

        private void Register(MonoBehaviour behaviour)
        {
            behaviours.Add(behaviour);
            Registers.Instance.RegisterBehaviour(behaviour);
        }
    }
}
