using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    public sealed class ShooterEntity : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private ShooterTransformObserver.TransformMobility mobility;

        private readonly List<MonoBehaviour> allComponents = new();
        private readonly List<MonoBehaviour> nativeComponents = new();
        private readonly List<Behaviour> switchableComponents = new();
        private readonly HashSet<Behaviour> switchedOffComponents = new();
        private Vector2Int? chunk;

        public ShooterTransformObserver.TransformMobility Mobility => mobility;

        public bool IsOptimized { get; private set; }

        private void Awake()
        {
            foreach (Component attached in GetComponents<Component>())
            {
                if (attached is not Behaviour component || component is ShooterComponent or ShooterNetworkComponent) continue;

                if (component is MonoBehaviour nativeComponent)
                {
                    nativeComponents.Add(nativeComponent);
                    NotifyComponentAttached(nativeComponent);
                }
                else
                {
                    AddSwitchable(component);
                }
            }

            // Not a RequireComponent: Unity creates a required component before the one requiring it, and the
            // observer would wake up with no entity to report to
            gameObject.AddComponent<ShooterTransformObserver>();
        }

        private void OnEnable()
        {
            foreach (MonoBehaviour component in nativeComponents)
            {
                if (component.enabled) NotifyComponentEnabled(component);
            }
        }

        private void OnDisable()
        {
            foreach (MonoBehaviour component in nativeComponents)
            {
                NotifyComponentDisabled(component);
            }
        }

        private void OnDestroy()
        {
            foreach (MonoBehaviour component in nativeComponents)
            {
                NotifyComponentDetached(component);
            }
        }

        // Our code never writes enabled of a component on an entity's object, it asks the entity: the component's own
        // wish is kept here and enabled is that wish unless the entity is optimized
        public void SetComponentEnabled(Behaviour component, bool componentEnabled)
        {
            if (componentEnabled) switchedOffComponents.Remove(component);
            else switchedOffComponents.Add(component);

            ApplyEnabled(component);
        }

        public void SetOptimized(bool optimized)
        {
            if (IsOptimized == optimized) return;

            IsOptimized = optimized;
            for (int i = 0; i < switchableComponents.Count; i++)
            {
                if (switchableComponents[i] != null) ApplyEnabled(switchableComponents[i]);
            }

            Log.Debug($"Entity {name} {(optimized ? "optimized" : "no longer optimized")}");
        }

        // The entity learns about its object only through Notify calls, direct rather than events: they come on every
        // enable, disable and move, and no delegates are kept per entity. ShooterComponent and ShooterNetworkComponent
        // send the component ones from their closed Unity messages, native components, without these bases, are
        // reported by the entity itself together with its object and whenever it switches one of them. A call may come
        // before this entity's Awake, as Unity doesn't order Awake between the components of one object, so the calls
        // rely only on fields set where they are declared
        public void NotifyComponentAttached(MonoBehaviour component)
        {
            allComponents.Add(component);
            AddSwitchable(component);
            Registers.Instance.RegisterComponent(component);
            if (chunk != null) Chunks.Instance.AddComponent(chunk.Value, component);
        }

        public void NotifyComponentEnabled(MonoBehaviour component)
        {
            Registers.Instance.ActivateComponent(component);
        }

        public void NotifyComponentDisabled(MonoBehaviour component)
        {
            Registers.Instance.DeactivateComponent(component);
        }

        public void NotifyComponentDetached(MonoBehaviour component)
        {
            allComponents.Remove(component);
            switchableComponents.Remove(component);
            switchedOffComponents.Remove(component);
            Registers.Instance.UnregisterComponent(component);
            if (chunk != null) Chunks.Instance.TryRemoveComponent(chunk.Value, component);
        }

        public void NotifyMoved()
        {
            Vector2Int movedTo = Chunks.ChunkOf(transform.position);
            if (chunk != null && chunk.Value == movedTo) return;

            foreach (MonoBehaviour component in allComponents)
            {
                if (chunk != null) Chunks.Instance.TryRemoveComponent(chunk.Value, component);
                Chunks.Instance.AddComponent(movedTo, component);
            }

            Log.Debug($"Entity {name} moved to chunk {movedTo}");
            chunk = movedTo;
        }

        private void AddSwitchable(Behaviour component)
        {
            if (component == this) return;

            switchableComponents.Add(component);
            if (!component.enabled) switchedOffComponents.Add(component);
            if (IsOptimized) component.enabled = false;
        }

        private void ApplyEnabled(Behaviour component)
        {
            component.enabled = !IsOptimized && !switchedOffComponents.Contains(component);
            if (component is not MonoBehaviour nativeComponent || !nativeComponents.Contains(nativeComponent)) return;

            if (component.enabled && isActiveAndEnabled) NotifyComponentEnabled(nativeComponent);
            else NotifyComponentDisabled(nativeComponent);
        }
    }
}
