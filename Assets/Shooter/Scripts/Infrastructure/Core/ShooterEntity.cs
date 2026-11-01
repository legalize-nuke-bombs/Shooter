using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
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

        private void Awake()
        {
            foreach (Component component in GetComponents<Component>())
            {
                if (component is IShooterComponent) continue;

                if (component is Behaviour behaviour) NotifyComponentAttached(behaviour);
                else AddEntry(component);
            }

            // Not a RequireComponent: Unity creates a required component before the one requiring it, and the
            // observer would wake up with no entity to report to
            gameObject.AddComponent<ShooterTransformObserver>();
        }

        private void OnEnable()
        {
            foreach (Entry entry in components)
            {
                if (entry.Component is Behaviour { enabled: true } behaviour and not IShooterComponent) NotifyComponentEnabled(behaviour);
            }
        }

        private void OnDisable()
        {
            foreach (Entry entry in components)
            {
                if (entry.Component is Behaviour behaviour and not IShooterComponent) NotifyComponentDisabled(behaviour);
            }
        }

        private void OnDestroy()
        {
            for (int i = components.Count - 1; i >= 0; i--)
            {
                if (components[i].Component is Behaviour behaviour and not IShooterComponent) NotifyComponentDetached(behaviour);
            }
        }

        // Our code never writes enabled of a component on an entity's object, it asks the entity: the entity keeps what
        // the component's code wants, and enabled is that unless the entity is optimized
        public void SetComponentEnabled(Component component, bool componentEnabled)
        {
            Entry entry = components.Find(candidate => candidate.Component == component);
            if (entry == null)
            {
                Log.Error($"Entity {name} was asked to switch {component.GetType().Name} {component.name}, which is not on its object or can't be switched");
                return;
            }

            entry.Enabled = componentEnabled;
            Apply(entry);
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

        // The entity learns about its object only through Notify calls, direct rather than events: they come on every
        // enable, disable and move, and no delegates are kept per entity. An IShooterComponent, as ShooterComponent and
        // ShooterNetworkComponent, sends the component ones from its closed Unity messages, every other behaviour, the
        // entity included, is reported by the entity itself together with its object and whenever the entity switches
        // it. A call may come before this entity's Awake, as Unity doesn't order Awake between the components of one
        // object, so the calls rely only on fields set where they are declared
        public void NotifyComponentAttached(Behaviour component)
        {
            AddEntry(component);
            Registers.Instance.RegisterComponent(component);
            if (chunk != null) Chunks.Instance.AddComponent(chunk.Value, component);
        }

        public void NotifyComponentEnabled(Behaviour component)
        {
            Registers.Instance.ActivateComponent(component);
        }

        public void NotifyComponentDisabled(Behaviour component)
        {
            Registers.Instance.DeactivateComponent(component);
        }

        public void NotifyComponentDetached(Behaviour component)
        {
            components.RemoveAll(entry => entry.Component == component);
            Registers.Instance.UnregisterComponent(component);
            if (chunk != null) Chunks.Instance.TryRemoveComponent(chunk.Value, component);
        }

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

        private void AddEntry(Component component)
        {
            bool? componentEnabled = GetEnabled(component);
            if (componentEnabled == null) return;

            components.Add(new Entry { Component = component, Enabled = componentEnabled.Value });
            if (IsOptimized) SetEnabled(component, false);
        }

        private void Apply(Entry entry)
        {
            SetEnabled(entry.Component, entry.Enabled && !IsOptimized);
            if (entry.Component is not Behaviour behaviour || behaviour is IShooterComponent) return;

            if (behaviour.isActiveAndEnabled) NotifyComponentEnabled(behaviour);
            else NotifyComponentDisabled(behaviour);
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

        private static void SetEnabled(Component component, bool componentEnabled)
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
