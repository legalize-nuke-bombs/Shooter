using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class ShooterTransformObserver : MonoBehaviour, IShooterLateUpdateable
    {
        private readonly List<ShooterTransformObserver> children = new();
        private ShooterEntity entity;
        private ShooterTransformObserver parent;
        private Vector3 lastPosition;

        private void Awake()
        {
            entity = GetComponent<ShooterEntity>();
        }

        private void Start()
        {
            Move();
            ChangeParent(FindParent());
            ShooterEntity.SetEnabled(this, entity.Mobility == TransformMobility.Dynamic);
        }

        private void OnTransformParentChanged()
        {
            ChangeParent(FindParent());
            Move();
        }

        private void OnDestroy()
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
            entity.NotifyMoved();
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
