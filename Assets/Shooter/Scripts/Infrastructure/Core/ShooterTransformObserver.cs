using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class ShooterTransformObserver : ShooterComponent, IShooterLateUpdateable
    {
        private readonly List<ShooterTransformObserver> children = new();
        private ShooterTransformObserver parent;
        private Vector3 lastPosition;

        private void Start()
        {
            Move();
            ChangeParent(FindParent());
            enabled = (Entity.Mobility == TransformMobility.Dynamic);
        }

        private void OnTransformParentChanged()
        {
            ChangeParent(FindParent());
            Move();
        }

        protected override void ShooterOnDestroy()
        {
            if (parent != null) parent.children.Remove(this);
        }

        public void ShooterLateUpdate()
        {
            if (transform.position != lastPosition) Move();
        }

        private void Move()
        {
            lastPosition = transform.position;
            Entity.NotifyMoved();
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
