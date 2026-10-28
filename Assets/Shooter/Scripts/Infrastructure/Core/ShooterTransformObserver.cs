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

        public event Action Placed;
        public event Action Moved;
        public event Action Removed;

        public TransformMobility Mobility
        {
            get => mobility;
            set => mobility = value;
        }

        private void Start()
        {
            lastPosition = transform.position;
            Placed?.Invoke();
            ChangeParent(FindParent());
            enabled = (mobility == TransformMobility.Dynamic);
        }

        private void OnTransformParentChanged()
        {
            ChangeParent(FindParent());
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
            return transform.parent == null ? null : transform.parent.GetComponentInParent<ShooterTransformObserver>(true);
        }

        public enum TransformMobility
        {
            Static,
            Dynamic
        }
    }
}
