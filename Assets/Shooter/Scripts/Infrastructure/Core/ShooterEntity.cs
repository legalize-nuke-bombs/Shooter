using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DefaultExecutionOrder(ExecutionOrder.Infrastructure2)]
    [DisallowMultipleComponent]
    public sealed class ShooterEntity : MonoBehaviour
    {
        public const string MobilityField = nameof(mobility);
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private ShooterTransformObserver.TransformMobility mobility;

        private readonly List<Entry> components = new();
        private Vector2Int? chunk;

        public ShooterTransformObserver.TransformMobility Mobility => mobility;

        public bool IsOptimized { get; private set; }

        public static void SetEnabled(Component component, bool componentEnabled)
        {
            component.GetComponent<ShooterEntity>().SetComponentEnabled(component, componentEnabled);
        }

        private void Awake()
        {
            foreach (Component component in GetComponents<Component>())
            {
                Attach(component);
            }

            AddComponent<ShooterTransformObserver>();
        }

        private void OnEnable()
        {
            foreach (Entry entry in components)
            {
                if (entry.Component is Behaviour { enabled: true } behaviour) Registers.Instance.ActivateComponent(behaviour);
            }
        }

        private void OnDisable()
        {
            foreach (Entry entry in components)
            {
                if (entry.Component is Behaviour behaviour) Registers.Instance.DeactivateComponent(behaviour);
            }
        }

        private void OnDestroy()
        {
            foreach (Entry entry in components)
            {
                if (entry.Component is not Behaviour behaviour) continue;

                Registers.Instance.UnregisterComponent(behaviour);
                if (chunk != null) Chunks.Instance.TryRemoveComponent(chunk.Value, behaviour);
            }
        }

        public T AddComponent<T>() where T : Component
        {
            T component = gameObject.AddComponent<T>();
            Attach(component);
            return component;
        }

        public void SetOptimized(bool optimized)
        {
            if (IsOptimized == optimized) return;

            IsOptimized = optimized;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].Component != null) Apply(components[i]);
            }

            Log.Debug($"Entity {name} {(optimized ? "optimized" : "no longer optimized")}");
        }

        // An inverted delegate: by its meaning this is the observer's Moved event with the entity subscribed to it, but
        // it fires on every move of every dynamic entity, so the one subscriber is called directly and no delegate is
        // kept per entity
        public void NotifyMoved()
        {
            Vector2Int movedTo = Chunks.ChunkOf(transform.position);
            if (chunk != null && chunk.Value == movedTo) return;

            foreach (Entry entry in components)
            {
                if (entry.Component is not Behaviour behaviour) continue;

                if (chunk != null) Chunks.Instance.TryRemoveComponent(chunk.Value, behaviour);
                Chunks.Instance.AddComponent(movedTo, behaviour);
            }

            Log.Debug($"Entity {name} moved to chunk {movedTo}");
            chunk = movedTo;
        }

        // A component may ask to be switched from its own Awake, before this entity's Awake has found it, as Unity
        // doesn't order Awake between the components of one object: it is taken then, and found again later is skipped
        private void SetComponentEnabled(Component component, bool componentEnabled)
        {
            Entry entry = Attach(component);
            if (entry == null)
            {
                Log.Error($"Entity {name} was asked to switch {component.GetType().Name} {component.name}, which can't be switched");
                return;
            }

            entry.Enabled = componentEnabled;
            Apply(entry);
        }

        private Entry Attach(Component component)
        {
            Entry entry = components.Find(candidate => candidate.Component == component);
            if (entry != null) return entry;

            bool? componentEnabled = GetEnabled(component);
            if (componentEnabled == null) return null;

            entry = new Entry { Component = component, Enabled = componentEnabled.Value };
            components.Add(entry);
            if (IsOptimized) Switch(component, false);
            if (component is not Behaviour behaviour) return entry;

            Registers.Instance.RegisterComponent(behaviour);
            if (chunk != null) Chunks.Instance.AddComponent(chunk.Value, behaviour);
            if (IsActive(behaviour)) Registers.Instance.ActivateComponent(behaviour);
            return entry;
        }

        private void Apply(Entry entry)
        {
            Switch(entry.Component, entry.Enabled && !IsOptimized);
            if (entry.Component is not Behaviour behaviour) return;

            if (IsActive(behaviour)) Registers.Instance.ActivateComponent(behaviour);
            else Registers.Instance.DeactivateComponent(behaviour);
        }

        // Not isActiveAndEnabled: it stays false in a component's Awake, before Unity calls its OnEnable
        private static bool IsActive(Behaviour behaviour)
        {
            return behaviour.enabled && behaviour.gameObject.activeInHierarchy;
        }

        // Unity has no common enabled: each family of components declares its own, and the rest can't be switched
        private static bool? GetEnabled(Component component)
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

        private static void Switch(Component component, bool componentEnabled)
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
            public bool Enabled;
        }
    }
}
