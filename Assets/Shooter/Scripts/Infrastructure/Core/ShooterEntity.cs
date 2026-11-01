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
        [SerializeField] private OptimizationLevelAsset optimizationLevel;

        private readonly List<Entry> components = new();
        private Vector2Int? chunk;

        public ShooterTransformObserver.TransformMobility Mobility => mobility;

        public bool IsOptimized { get; private set; }

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
            FindOrRegister(component);
            return component;
        }

        public void SetOptimized(bool optimized)
        {
            if (IsOptimized == optimized) return;

            IsOptimized = optimized;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].Component != null) UpdateUnityEnabled(components[i]);
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

        private Entry FindOrRegister(Component component)
        {
            Entry entry = components.Find(candidate => candidate.Component == component);
            if (entry != null) return entry;

            bool? componentEnabled = ReadUnityEnabled(component);
            if (componentEnabled == null) return null;

            entry = new Entry { Component = component, WantsEnabled = componentEnabled.Value };
            components.Add(entry);
            if (IsOptimized && IsOptimizable(component)) WriteUnityEnabled(component, false);
            if (component is not Behaviour behaviour) return entry;

            Registers.Instance.RegisterComponent(behaviour);
            if (chunk != null) Chunks.Instance.AddComponent(chunk.Value, behaviour);
            if (IsEnabledAndActive(behaviour)) Registers.Instance.ActivateComponent(behaviour);
            return entry;
        }

        private void UpdateUnityEnabled(Entry entry)
        {
            WriteUnityEnabled(entry.Component, entry.WantsEnabled && !(IsOptimized && IsOptimizable(entry.Component)));
            if (entry.Component is not Behaviour behaviour) return;

            if (IsEnabledAndActive(behaviour)) Registers.Instance.ActivateComponent(behaviour);
            else Registers.Instance.DeactivateComponent(behaviour);
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
    }
}
