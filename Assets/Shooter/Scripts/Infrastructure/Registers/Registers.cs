using System;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.InfrastructureSystemCore)]
    public class Registers : MonoBehaviour
    {
        private const float CollectIntervalSeconds = 2f;
        private static readonly Journal Log = Logs.Here();

        public static Registers Instance { get; private set; }

        public int TypeCount => components.KeyCount;

        public int UsedSlots => components.UsedSlots + activeComponents.UsedSlots;

        public int Capacity => components.Capacity + activeComponents.Capacity;

        private readonly TMap components = new();
        private readonly TMap activeComponents = new();
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
            components.CollectGarbage();
            activeComponents.CollectGarbage();
        }

        public void RegisterComponent(Behaviour component)
        {
            if (components.TryAdd(component))
            {
                Log.Debug($"{component.GetType().Name} {component.name} registered");
            }
        }

        public void UnregisterComponent(Behaviour component)
        {
            components.Remove(component);
            Log.Debug($"{component.GetType().Name} {component.name} unregistered");
        }

        public void ActivateComponent(Behaviour component)
        {
            if (activeComponents.TryAdd(component))
            {
                Log.Debug($"{component.GetType().Name} {component.name} activated");
            }
        }

        public void DeactivateComponent(Behaviour component)
        {
            activeComponents.Remove(component);
            Log.Debug($"{component.GetType().Name} {component.name} deactivated");
        }

        public TArray.View<T> GetAll<T>(RegisterInactivePolicy inactivePolicy) where T : class
        {
            TMap registered = inactivePolicy == RegisterInactivePolicy.Include ? components : activeComponents;
            return registered.GetAll<T>();
        }

        public int CountOf(Type type, RegisterInactivePolicy inactivePolicy)
        {
            TMap registered = inactivePolicy == RegisterInactivePolicy.Include ? components : activeComponents;
            return registered.CountOf(type);
        }

        public void FindLargestClasses(Type[] classes, int[] counts)
        {
            components.FindLargestClasses(classes, counts);
        }

        public T Get<T>() where T : class
        {
            TArray.View<T> members = GetAll<T>(RegisterInactivePolicy.Include);
            if (members.Count != 1 || !members.TryGetFirst(out T member)) throw new InvalidOperationException($"{members.Count} {typeof(T).Name} registered instead of one");
            return member;
        }
    }
}
