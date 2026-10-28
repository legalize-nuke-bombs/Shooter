using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Registration)]
    public sealed class ShooterObject : MonoBehaviour
    {
        private readonly List<MonoBehaviour> behaviours = new();

        private void Awake()
        {
            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour != null) Register(behaviour);
            }
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
            }
        }

        public T AddComponent<T>() where T : MonoBehaviour
        {
            T behaviour = gameObject.AddComponent<T>();
            Register(behaviour);
            if (isActiveAndEnabled) Registers.Instance.ActivateBehaviour(behaviour);
            return behaviour;
        }

        private void Register(MonoBehaviour behaviour)
        {
            behaviours.Add(behaviour);
            Registers.Instance.RegisterBehaviour(behaviour);
        }
    }
}
