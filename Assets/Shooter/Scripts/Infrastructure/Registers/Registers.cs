using System;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DefaultExecutionOrder(ExecutionOrder.Infrastructure)]
    public class Registers : MonoBehaviour
    {
        private const float CollectIntervalSeconds = 2f;
        private static readonly Journal Log = Logs.Here();

        public static Registers Instance { get; private set; }

        public int TypeCount => behaviours.KeyCount;

        public int UsedSlots => behaviours.UsedSlots + activeBehaviours.UsedSlots;

        public int Capacity => behaviours.Capacity + activeBehaviours.Capacity;

        private readonly TMap behaviours = new();
        private readonly TMap activeBehaviours = new();
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
            behaviours.CollectGarbage();
            activeBehaviours.CollectGarbage();
        }

        public void RegisterBehaviour(IShooterBehaviour behaviour)
        {
            behaviours.Add(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} registered");
        }

        public void UnregisterBehaviour(IShooterBehaviour behaviour)
        {
            behaviours.Remove(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} unregistered");
        }

        public void ActivateBehaviour(IShooterBehaviour behaviour)
        {
            activeBehaviours.Add(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} activated");
        }

        public void DeactivateBehaviour(IShooterBehaviour behaviour)
        {
            activeBehaviours.Remove(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} deactivated");
        }

        public TArray.View<T> GetAll<T>(RegisterInactivePolicy inactivePolicy) where T : class
        {
            TMap registered = inactivePolicy == RegisterInactivePolicy.Include ? behaviours : activeBehaviours;
            return registered.GetAll<T>();
        }

        public int CountOf(Type type, RegisterInactivePolicy inactivePolicy)
        {
            TMap registered = inactivePolicy == RegisterInactivePolicy.Include ? behaviours : activeBehaviours;
            return registered.CountOf(type);
        }

        public void FindLargestClasses(Type[] classes, int[] counts)
        {
            behaviours.FindLargestClasses(classes, counts);
        }

        public T Get<T>() where T : class
        {
            TArray.View<T> members = GetAll<T>(RegisterInactivePolicy.Include);
            if (members.Count != 1 || !members.TryGetFirst(out T member)) throw new InvalidOperationException($"{members.Count} {typeof(T).Name} registered instead of one");
            return member;
        }
    }
}
