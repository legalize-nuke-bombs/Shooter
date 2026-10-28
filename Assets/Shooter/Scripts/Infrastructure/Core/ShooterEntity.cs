using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    public sealed class ShooterEntity : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private ShooterTransformObserver.TransformMobility mobility;

        private readonly List<MonoBehaviour> components = new();
        private readonly List<MonoBehaviour> plainComponents = new();
        private Vector2Int? chunk;

        public ShooterTransformObserver.TransformMobility Mobility => mobility;

        private void Awake()
        {
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (component == null || component is ShooterComponent or ShooterNetworkComponent) continue;

                plainComponents.Add(component);
                NotifyComponentAttached(component);
            }

            // Not a RequireComponent: Unity creates a required component before the one requiring it, and the
            // observer would wake up with no entity to report to
            gameObject.AddComponent<ShooterTransformObserver>();
        }

        private void OnEnable()
        {
            foreach (MonoBehaviour component in plainComponents)
            {
                NotifyComponentEnabled(component);
            }
        }

        private void OnDisable()
        {
            foreach (MonoBehaviour component in plainComponents)
            {
                NotifyComponentDisabled(component);
            }
        }

        private void OnDestroy()
        {
            foreach (MonoBehaviour component in plainComponents)
            {
                NotifyComponentDetached(component);
            }
        }

        // The entity learns about its object only through Notify calls, direct rather than events: they come on
        // every enable, disable and move, and no delegates are kept per entity. ShooterComponent and
        // ShooterNetworkComponent send the component ones from their closed Unity messages, plain components are
        // reported by the entity itself together with its object, since Unity doesn't tell when one of them alone
        // is enabled or disabled. A call may come before this entity's Awake, as Unity doesn't order Awake between
        // the components of one object, so the calls rely only on fields set where they are declared
        public void NotifyComponentAttached(MonoBehaviour component)
        {
            components.Add(component);
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
            components.Remove(component);
            Registers.Instance.UnregisterComponent(component);
            if (chunk != null) Chunks.Instance.TryRemoveComponent(chunk.Value, component);
        }

        public void NotifyMoved()
        {
            Vector2Int movedTo = Chunks.ChunkOf(transform.position);
            if (chunk != null && chunk.Value == movedTo) return;

            foreach (MonoBehaviour component in components)
            {
                if (chunk != null) Chunks.Instance.TryRemoveComponent(chunk.Value, component);
                Chunks.Instance.AddComponent(movedTo, component);
            }

            Log.Debug($"Entity {name} moved to chunk {movedTo}");
            chunk = movedTo;
        }
    }
}
