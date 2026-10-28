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

        public void RegisterBehaviour(MonoBehaviour behaviour)
        {
            behaviours.Add(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.name} registered");
        }

        public void UnregisterBehaviour(MonoBehaviour behaviour)
        {
            behaviours.Remove(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.name} unregistered");
        }

        public void ActivateBehaviour(MonoBehaviour behaviour)
        {
            activeBehaviours.Add(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.name} activated");
        }

        public void DeactivateBehaviour(MonoBehaviour behaviour)
        {
            activeBehaviours.Remove(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.name} deactivated");
        }

        public View<T> GetAll<T>(RegisterInactivePolicy inactivePolicy) where T : class
        {
            TMap registered = inactivePolicy == RegisterInactivePolicy.Include ? behaviours : activeBehaviours;
            return new View<T>(registered.GetAll<T>(), inactivePolicy);
        }

        public int CountOf(Type type, RegisterInactivePolicy inactivePolicy)
        {
            TMap registered = inactivePolicy == RegisterInactivePolicy.Include ? behaviours : activeBehaviours;
            return new View<MonoBehaviour>(registered.GetAll(type), inactivePolicy).Count;
        }

        public void FindLargestClasses(Type[] classes, int[] counts)
        {
            behaviours.FindLargestClasses(classes, counts);
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
