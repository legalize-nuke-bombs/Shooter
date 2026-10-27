using System;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DefaultExecutionOrder(ExecutionOrder.Infrastructure)]
    public class Registers : MonoBehaviour
    {
        private const float CollectIntervalSeconds = 2f;
        private static readonly Journal Log = Logs.Here();

        private static Registers instance;

        private readonly TMap behaviours = new();
        private readonly TMap activeBehaviours = new();
        private float nextCollectTime;

        private void Awake()
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextCollectTime) return;

            nextCollectTime = Time.unscaledTime + CollectIntervalSeconds;
            behaviours.CollectGarbage();
            activeBehaviours.CollectGarbage();
        }

        public static void RegisterBehaviour(IShooterBehaviour behaviour)
        {
            instance.behaviours.Add(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} registered");
        }

        public static void UnregisterBehaviour(IShooterBehaviour behaviour)
        {
            instance.behaviours.Remove(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} unregistered");
        }

        public static void ActivateBehaviour(IShooterBehaviour behaviour)
        {
            instance.activeBehaviours.Add(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} activated");
        }

        public static void DeactivateBehaviour(IShooterBehaviour behaviour)
        {
            instance.activeBehaviours.Remove(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} deactivated");
        }

        public static TArray.View<T> GetAll<T>(RegisterInactivePolicy inactivePolicy) where T : class
        {
            TMap behaviours = inactivePolicy == RegisterInactivePolicy.Include ? instance.behaviours : instance.activeBehaviours;
            return behaviours.GetAll<T>();
        }

        public static T Get<T>() where T : class
        {
            TArray.View<T> members = GetAll<T>(RegisterInactivePolicy.Include);
            if (members.Count != 1 || !members.TryGetFirst(out T member)) throw new InvalidOperationException($"{members.Count} {typeof(T).Name} registered instead of one");
            return member;
        }
    }
}
