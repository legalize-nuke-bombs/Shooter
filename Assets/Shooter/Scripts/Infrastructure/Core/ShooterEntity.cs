using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    public sealed class ShooterEntity : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private ShooterTransformObserver.TransformMobility mobility;

        private readonly List<Entry> components = new();
        private Vector2Int? chunk;

        public ShooterTransformObserver.TransformMobility Mobility => mobility;

        public bool IsOptimized { get; private set; }

        private void Awake()
        {
            foreach (Component attached in GetComponents<Component>())
            {
                if (attached is not Behaviour component || ReportsItself(component)) continue;

                NotifyComponentAttached(component);
            }

            // Not a RequireComponent: Unity creates a required component before the one requiring it, and the
            // observer would wake up with no entity to report to
            gameObject.AddComponent<ShooterTransformObserver>();
        }

        private void OnEnable()
        {
            foreach (Entry entry in components)
            {
                if (!ReportsItself(entry.Component) && entry.Component.enabled) NotifyComponentEnabled(entry.Component);
            }
        }

        private void OnDisable()
        {
            foreach (Entry entry in components)
            {
                if (!ReportsItself(entry.Component)) NotifyComponentDisabled(entry.Component);
            }
        }

        private void OnDestroy()
        {
            for (int i = components.Count - 1; i >= 0; i--)
            {
                if (!ReportsItself(components[i].Component)) NotifyComponentDetached(components[i].Component);
            }
        }

        // Our code never writes enabled of a component on an entity's object, it asks the entity: the entity keeps what
        // the component's code wants, and enabled is that unless the entity is optimized
        public void SetComponentEnabled(Behaviour component, bool componentEnabled)
        {
            Entry entry = components.Find(candidate => candidate.Component == component);
            if (entry == null)
            {
                Log.Error($"Entity {name} was asked to switch {component.GetType().Name} {component.name}, which is not on its object");
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
        // enable, disable and move, and no delegates are kept per entity. ShooterComponent and ShooterNetworkComponent
        // send the component ones from their closed Unity messages, every other component, the entity included, is
        // reported by the entity itself together with its object and whenever the entity switches it. A call may come
        // before this entity's Awake, as Unity doesn't order Awake between the components of one object, so the calls
        // rely only on fields set where they are declared
        public void NotifyComponentAttached(Behaviour component)
        {
            components.Add(new Entry { Component = component, Enabled = component.enabled });
            if (IsOptimized) component.enabled = false;
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
                if (chunk != null) Chunks.Instance.TryRemoveComponent(chunk.Value, entry.Component);
                Chunks.Instance.AddComponent(movedTo, entry.Component);
            }

            Log.Debug($"Entity {name} moved to chunk {movedTo}");
            chunk = movedTo;
        }

        private void Apply(Entry entry)
        {
            entry.Component.enabled = entry.Enabled && !IsOptimized;
            if (ReportsItself(entry.Component)) return;

            if (entry.Component.isActiveAndEnabled) NotifyComponentEnabled(entry.Component);
            else NotifyComponentDisabled(entry.Component);
        }

        private static bool ReportsItself(Behaviour component)
        {
            return component is ShooterComponent or ShooterNetworkComponent;
        }

        private sealed class Entry
        {
            public Behaviour Component;
            public bool Enabled;
        }
    }
}
