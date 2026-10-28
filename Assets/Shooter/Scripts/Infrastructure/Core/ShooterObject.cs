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
            foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour.GetComponentInParent<ShooterObject>(true) != this) continue;

                Register(behaviour);
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

        public T AddComponent<T>(GameObject target) where T : MonoBehaviour
        {
            T behaviour = target.AddComponent<T>();
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
