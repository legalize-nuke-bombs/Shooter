using System;
using System.Collections.Generic;
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
        private readonly Dictionary<Type, Subscribers> subscribersByKey = new();
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
                RaiseRegistered(component);
            }
        }

        public void SubscribeRegistered<T>(Action<T> onRegistered) where T : class
        {
            if (!subscribersByKey.TryGetValue(typeof(T), out Subscribers subscribers))
            {
                subscribers = new Subscribers<T>();
                subscribersByKey[typeof(T)] = subscribers;
            }

            ((Subscribers<T>)subscribers).Registered += onRegistered;
        }

        public void UnsubscribeRegistered<T>(Action<T> onRegistered) where T : class
        {
            if (subscribersByKey.TryGetValue(typeof(T), out Subscribers subscribers))
            {
                ((Subscribers<T>)subscribers).Registered -= onRegistered;
            }
        }

        private void RaiseRegistered(Behaviour component)
        {
            if (subscribersByKey.Count == 0) return;

            foreach (Type key in TMap.GetKeys(component.GetType()))
            {
                if (subscribersByKey.TryGetValue(key, out Subscribers subscribers)) subscribers.RaiseRegistered(component);
            }
        }

        public void UnregisterComponent(Behaviour component)
        {
            if (components.TryRemove(component))
            {
                Log.Debug($"{component.GetType().Name} {component.name} unregistered");
            }
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
            if (activeComponents.TryRemove(component))
            {
                Log.Debug($"{component.GetType().Name} {component.name} deactivated");
            }
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

        private abstract class Subscribers
        {
            public abstract void RaiseRegistered(object member);
        }

        private sealed class Subscribers<T> : Subscribers where T : class
        {
            public event Action<T> Registered;

            public override void RaiseRegistered(object member)
            {
                Registered?.Invoke((T)member);
            }
        }
    }
}
