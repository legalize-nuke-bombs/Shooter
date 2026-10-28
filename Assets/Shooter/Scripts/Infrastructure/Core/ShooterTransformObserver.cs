using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    public class ShooterTransformObserver : MonoBehaviour, IShooterLateUpdateable
    {
        [SerializeField] private TransformMobility mobility;

        private readonly List<ShooterTransformObserver> children = new();
        private ShooterTransformObserver parent;
        private Vector3 lastPosition;
        private bool started;

        public event Action Placed;
        public event Action Moved;
        public event Action Removed;

        public TransformMobility Mobility
        {
            get => mobility;
            set => mobility = value;
        }

        private bool ShouldPoll => (mobility == TransformMobility.Dynamic) && (parent == null);

        private void Start()
        {
            started = true;
            lastPosition = transform.position;
            Placed?.Invoke();
            ChangeParent(FindParent());
            enabled = ShouldPoll;
        }

        private void OnTransformParentChanged()
        {
            if (!started || mobility == TransformMobility.Static) return;

            ChangeParent(FindParent());
            enabled = ShouldPoll;
            Move();
        }

        private void OnDestroy()
        {
            if (parent != null) parent.children.Remove(this);
            Removed?.Invoke();
        }

        public void ShooterLateUpdate()
        {
            if (transform.position != lastPosition) Move();
        }

        private void Move()
        {
            lastPosition = transform.position;
            Moved?.Invoke();
            foreach (ShooterTransformObserver child in children)
            {
                child.Move();
            }
        }

        private void ChangeParent(ShooterTransformObserver newParent)
        {
            if (parent != null) parent.children.Remove(this);

            parent = newParent;
            if (parent != null) parent.children.Add(this);
        }

        private ShooterTransformObserver FindParent()
        {
            if (mobility == TransformMobility.Static) return null;

            for (Transform ancestor = transform.parent; ancestor != null; ancestor = ancestor.parent)
            {
                if (ancestor.TryGetComponent(out ShooterTransformObserver observer) && observer.mobility == TransformMobility.Dynamic) return observer;
            }

            return null;
        }

        public enum TransformMobility
        {
            Static,
            Dynamic
        }
    }
}
