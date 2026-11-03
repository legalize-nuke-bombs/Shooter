using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DefaultExecutionOrder(ExecutionOrder.InfrastructureEntity)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShooterTransformObserver))]
    public sealed class ShooterEntity : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private OptimizationLevelAsset optimizationLevel;
        public OptimizationLevelAsset OptimizationLevel => optimizationLevel;

        private readonly List<Entry> components = new();
        private ShooterTransformObserver observer;
        private Vector2Int? chunk;
        private bool isOptimizedSelf;
        private bool isOptimizedInHierarchy;

        public static ComponentStatus GetComponentStatus(Component component)
        {
            ShooterEntity entity = component.GetComponent<ShooterEntity>();
            Entry entry = entity.FindOrRegister(component);
            if (entry == null)
            {
                Log.Error($"Entity {entity.name} was asked the status of {component.GetType().Name} {component.name}, which can't be switched");
                return ComponentStatus.Enabled;
            }

            if (!entry.WantsEnabled || !component.gameObject.activeInHierarchy) return ComponentStatus.Disabled;
            return entity.isOptimizedInHierarchy && IsOptimizable(component) ? ComponentStatus.Optimized : ComponentStatus.Enabled;
        }

        public static void SetEnabled(Component component, bool componentEnabled)
        {
            ShooterEntity entity = component.GetComponent<ShooterEntity>();
            Entry entry = entity.FindOrRegister(component);
            if (entry == null)
            {
                Log.Error($"Entity {entity.name} was asked to switch {component.GetType().Name} {component.name}, which can't be switched");
                return;
            }

            entry.WantsEnabled = componentEnabled;
            entity.UpdateUnityEnabled(entry);
        }

        private void Awake()
        {
            foreach (Component component in GetComponents<Component>())
            {
                FindOrRegister(component);
            }

            observer = GetComponent<ShooterTransformObserver>();
            observer.Moved += HandleMoved;
            observer.ParentChanged += UpdateOptimizedInHierarchy;
        }

        // Not in Awake: a component switched off before its Start never gets one, and the observer starts earlier to
        // place the entity in its chunk and under its parent before the entity may switch the observer off
        private void Start()
        {
            isOptimizedSelf = optimizationLevel && optimizationLevel.Optimizable;
            UpdateOptimizedInHierarchy();
        }

        private void OnEnable()
        {
            foreach (Entry entry in components)
            {
                if (entry.Component is Behaviour { enabled: true } behaviour) ActivateComponent(behaviour);
            }
        }

        private void OnDisable()
        {
            foreach (Entry entry in components)
            {
                if (entry.Component is Behaviour behaviour) DeactivateComponent(behaviour);
            }
        }

        private void OnDestroy()
        {
            observer.Moved -= HandleMoved;
            observer.ParentChanged -= UpdateOptimizedInHierarchy;
            foreach (Entry entry in components)
            {
                if (entry.Component is not Behaviour behaviour) continue;

                Registers.Instance.UnregisterComponent(behaviour);
                if (chunk != null)  Registers.Instance.Get<Chunks>().RemoveComponent(chunk.Value, behaviour);
            }
        }

        public T AddComponent<T>() where T : Component
        {
            T component = gameObject.AddComponent<T>();
            FindOrRegister(component);
            return component;
        }

        public void SetOptimized(bool optimized)
        {
            if (isOptimizedSelf == optimized) return;

            isOptimizedSelf = optimized;
            UpdateOptimizedInHierarchy();
        }

        private void UpdateOptimizedInHierarchy()
        {
            bool optimized = isOptimizedSelf || (observer.Parent != null && observer.Parent.GetComponent<ShooterEntity>().isOptimizedInHierarchy);
            if (isOptimizedInHierarchy == optimized) return;

            isOptimizedInHierarchy = optimized;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].Component != null) UpdateUnityEnabled(components[i]);
            }

            Log.Debug($"Entity {name} {(optimized ? "optimized" : "no longer optimized")}");
            for (int i = 0; i < observer.Children.Count; i++)
            {
                observer.Children[i].GetComponent<ShooterEntity>().UpdateOptimizedInHierarchy();
            }
        }

        private void HandleMoved()
        {
            Vector2Int movedTo = Chunks.ChunkOf(transform.position);
            if (chunk != null && chunk.Value == movedTo) return;

            foreach (Entry entry in components)
            {
                if (entry.Component is not Behaviour behaviour) continue;

                if (chunk != null)  Registers.Instance.Get<Chunks>().RemoveComponent(chunk.Value, behaviour);
                Registers.Instance.Get<Chunks>().AddComponent(movedTo, behaviour);
                if (IsEnabledAndActive(behaviour))  Registers.Instance.Get<Chunks>().ActivateComponent(movedTo, behaviour);
            }

            Log.Debug($"Entity {name} moved to chunk {movedTo}");
            chunk = movedTo;
        }

        private Entry FindOrRegister(Component component)
        {
            Entry entry = components.Find(candidate => candidate.Component == component);
            if (entry != null) return entry;

            bool? componentEnabled = ReadUnityEnabled(component);
            if (componentEnabled == null) return null;

            entry = new Entry { Component = component, WantsEnabled = componentEnabled.Value };
            components.Add(entry);
            if (isOptimizedInHierarchy && IsOptimizable(component)) WriteUnityEnabled(component, false);
            if (component is not Behaviour behaviour) return entry;

            Registers.Instance.RegisterComponent(behaviour);
            if (chunk != null)  Registers.Instance.Get<Chunks>().AddComponent(chunk.Value, behaviour);
            if (IsEnabledAndActive(behaviour)) ActivateComponent(behaviour);
            return entry;
        }

        private void UpdateUnityEnabled(Entry entry)
        {
            WriteUnityEnabled(entry.Component, entry.WantsEnabled && !(isOptimizedInHierarchy && IsOptimizable(entry.Component)));
            if (entry.Component is not Behaviour behaviour) return;

            if (IsEnabledAndActive(behaviour)) ActivateComponent(behaviour);
            else DeactivateComponent(behaviour);
        }

        private void ActivateComponent(Behaviour component)
        {
            Registers.Instance.ActivateComponent(component);
            if (chunk != null) Registers.Instance.Get<Chunks>().ActivateComponent(chunk.Value, component);
        }

        private void DeactivateComponent(Behaviour component)
        {
            Registers.Instance.DeactivateComponent(component);
            if (chunk != null)  Registers.Instance.Get<Chunks>().TryDeactivateComponent(chunk.Value, component);
        }

        // Not isActiveAndEnabled: it stays false in a component's Awake, before Unity calls its OnEnable
        private static bool IsEnabledAndActive(Behaviour behaviour)
        {
            return behaviour.enabled && behaviour.gameObject.activeInHierarchy;
        }

        // No Collider: switching colliders on and off leaks PhysX memory, about 15 MB per 214 colliders a cycle in the
        // editor and in a build (measured 1.11), so optimization leaves them as they are
        private static bool IsOptimizable(Component component)
        {
            return component switch
            {
                Behaviour or Renderer or LODGroup => true,
                _ => false
            };
        }

        private static bool? ReadUnityEnabled(Component component)
        {
            return component switch
            {
                Behaviour behaviour => behaviour.enabled,
                Renderer renderer => renderer.enabled,
                Collider collider => collider.enabled,
                LODGroup lodGroup => lodGroup.enabled,
                _ => null
            };
        }

        private static void WriteUnityEnabled(Component component, bool componentEnabled)
        {
            switch (component)
            {
                case Behaviour behaviour:
                    behaviour.enabled = componentEnabled;
                    break;
                case Renderer renderer:
                    renderer.enabled = componentEnabled;
                    break;
                case Collider collider:
                    collider.enabled = componentEnabled;
                    break;
                case LODGroup lodGroup:
                    lodGroup.enabled = componentEnabled;
                    break;
            }
        }

        private sealed class Entry
        {
            public Component Component;
            public bool WantsEnabled;
        }

        public enum ComponentStatus
        {
            Enabled,
            Disabled,
            Optimized
        }
    }
}
