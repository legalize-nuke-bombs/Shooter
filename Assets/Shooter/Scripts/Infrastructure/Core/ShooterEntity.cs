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

        private readonly List<Entry> components = new();
        private ShooterTransformObserver observer;
        private Chunks chunks;
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

            bool changed = entry.WantsEnabled != componentEnabled;
            entry.WantsEnabled = componentEnabled;
            entity.UpdateUnityEnabled(entry);
            if (!changed || component is not IWantsEnabledHandler handler) return;

            if (componentEnabled) handler.OnWantsEnabled();
            else handler.OnWantsDisabled();
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

        private void OnEnable()
        {
            foreach (Entry entry in components)
            {
                if (IsRegistrable(entry.Component) && ComponentSwitcher.IsEnabled(entry.Component)) ActivateComponent(entry.Component);
            }
        }

        private void OnDisable()
        {
            foreach (Entry entry in components)
            {
                if (IsRegistrable(entry.Component)) DeactivateComponent(entry.Component);
            }
        }

        private void OnDestroy()
        {
            observer.Moved -= HandleMoved;
            observer.ParentChanged -= UpdateOptimizedInHierarchy;
            foreach (Entry entry in components)
            {
                if (!IsRegistrable(entry.Component)) continue;

                Registers.Instance.UnregisterComponent(entry.Component);
                if (chunk != null) chunks.Remove(chunk.Value, entry.Component);
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

            // Not in Awake: in Boot an entity may wake before the one that registers Chunks, both have the same order
            if (chunk == null) chunks = Registers.Instance.Get<Chunks>();

            Vector2Int? movedFrom = chunk;
            chunk = movedTo;
            for (int i = 0; i < components.Count; i++)
            {
                Component component = components[i].Component;
                if (!IsRegistrable(component)) continue;

                chunks.Add(movedTo, component);
                if (IsEnabledAndActive(component)) chunks.ActivateComponent(movedTo, component);
                if (movedFrom != null) chunks.Remove(movedFrom.Value, component);
            }

            Log.Debug($"Entity {name} moved to chunk {movedTo}");
        }

        private Entry FindOrRegister(Component component)
        {
            Entry entry = components.Find(candidate => candidate.Component == component);
            if (entry != null) return entry;
            if (!ComponentSwitcher.CanSwitch(component)) return null;

            entry = new Entry { Component = component, WantsEnabled = ComponentSwitcher.IsEnabled(component) };
            components.Add(entry);
            if (isOptimizedInHierarchy && IsOptimizable(component)) ComponentSwitcher.SetEnabled(component, false);
            if (!IsRegistrable(component)) return entry;

            Registers.Instance.RegisterComponent(component);
            if (chunk != null) chunks.Add(chunk.Value, component);
            if (IsEnabledAndActive(component)) ActivateComponent(component);
            return entry;
        }

        private void UpdateUnityEnabled(Entry entry)
        {
            ComponentSwitcher.SetEnabled(entry.Component, entry.WantsEnabled && !(isOptimizedInHierarchy && IsOptimizable(entry.Component)));
            if (!IsRegistrable(entry.Component)) return;

            if (IsEnabledAndActive(entry.Component)) ActivateComponent(entry.Component);
            else DeactivateComponent(entry.Component);
        }

        private void ActivateComponent(Component component)
        {
            Registers.Instance.ActivateComponent(component);
            if (chunk != null) chunks.ActivateComponent(chunk.Value, component);
        }

        private void DeactivateComponent(Component component)
        {
            Registers.Instance.DeactivateComponent(component);
            if (chunk != null) chunks.TryDeactivateComponent(chunk.Value, component);
        }

        // Not isActiveAndEnabled: it stays false in a component's Awake, before Unity calls its OnEnable
        private static bool IsEnabledAndActive(Component component)
        {
            return ComponentSwitcher.IsEnabled(component) && component.gameObject.activeInHierarchy;
        }

        private static bool IsRegistrable(Component component)
        {
            return component is Behaviour or WindZone;
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
