using System;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Infrastructure)]
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

        public void RegisterComponent(MonoBehaviour component)
        {
            components.Add(component);
            Log.Debug($"{component.GetType().Name} {component.name} registered");
        }

        public void UnregisterComponent(MonoBehaviour component)
        {
            components.Remove(component);
            Log.Debug($"{component.GetType().Name} {component.name} unregistered");
        }

        public void ActivateComponent(MonoBehaviour component)
        {
            activeComponents.Add(component);
            Log.Debug($"{component.GetType().Name} {component.name} activated");
        }

        public void DeactivateComponent(MonoBehaviour component)
        {
            activeComponents.Remove(component);
            Log.Debug($"{component.GetType().Name} {component.name} deactivated");
        }

        public View<T> GetAll<T>(RegisterInactivePolicy inactivePolicy) where T : class
        {
            TMap registered = inactivePolicy == RegisterInactivePolicy.Include ? components : activeComponents;
            return new View<T>(registered.GetAll<T>(), inactivePolicy);
        }

        public int CountOf(Type type, RegisterInactivePolicy inactivePolicy)
        {
            TMap registered = inactivePolicy == RegisterInactivePolicy.Include ? components : activeComponents;
            return new View<MonoBehaviour>(registered.GetAll(type), inactivePolicy).Count;
        }

        public int CountWalked(Type type)
        {
            return activeComponents.CountOf(type);
        }

        public void FindLargestClasses(Type[] classes, int[] counts)
        {
            components.FindLargestClasses(classes, counts);
        }

        public T Get<T>() where T : class
        {
            View<T> members = GetAll<T>(RegisterInactivePolicy.Include);
            if (members.Count != 1 || !members.TryGetFirst(out T member)) throw new InvalidOperationException($"{members.Count} {typeof(T).Name} registered instead of one");
            return member;
        }

        public readonly struct View<T> where T : class
        {
            private readonly TArray.View<T> members;
            private readonly RegisterInactivePolicy inactivePolicy;

            public View(TArray.View<T> members, RegisterInactivePolicy inactivePolicy)
            {
                this.members = members;
                this.inactivePolicy = inactivePolicy;
            }

            public int Count
            {
                get
                {
                    if (inactivePolicy == RegisterInactivePolicy.Include) return members.Count;

                    int count = 0;
                    foreach (T member in this)
                    {
                        count++;
                    }

                    return count;
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(members.GetEnumerator(), inactivePolicy);
            }

            public bool TryGetFirst(out T first)
            {
                foreach (T member in this)
                {
                    first = member;
                    return true;
                }

                first = null;
                return false;
            }

            public ref struct Enumerator
            {
                private TArray.View<T>.Enumerator members;
                private readonly RegisterInactivePolicy inactivePolicy;

                public Enumerator(TArray.View<T>.Enumerator members, RegisterInactivePolicy inactivePolicy)
                {
                    this.members = members;
                    this.inactivePolicy = inactivePolicy;
                }

                public T Current => members.Current;

                public bool MoveNext()
                {
                    while (members.MoveNext())
                    {
                        if (inactivePolicy == RegisterInactivePolicy.Include || ((MonoBehaviour)(object)members.Current).isActiveAndEnabled) return true;
                    }

                    return false;
                }
            }
        }
    }
}
