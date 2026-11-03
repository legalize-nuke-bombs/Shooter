using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [AddComponentMenu("")]
    [DefaultExecutionOrder(ExecutionOrder.InfrastructureSystemCore)]
    [DisallowMultipleComponent]
    public class ShooterTransformObserver : MonoBehaviour, IShooterLateUpdateable
    {
        public const string MobilityField = nameof(mobility);

        [SerializeField] private TransformMobility mobility;

        private readonly List<ShooterTransformObserver> children = new();
        private ShooterTransformObserver parent;
        private Vector3 lastPosition;
        private bool isStarted;

        public ShooterTransformObserver Parent => parent;
        public IReadOnlyList<ShooterTransformObserver> Children => children;

        public event Action Moved;
        public event Action ParentChanged;

        private void Start()
        {
            isStarted = true;
            Move();
            ChangeParent(FindParent());
            ShooterEntity.SetEnabled(this, mobility == TransformMobility.Dynamic);
        }

        // Unity sends it to inactive objects too, before their Awake, as when a spawned rifle with inactive rounds is
        // put in a hand: an observer joins the tree only in its Start, when its entity is awake
        private void OnTransformParentChanged()
        {
            if (!isStarted) return;

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
            Moved?.Invoke();
            foreach (ShooterTransformObserver child in children)
            {
                child.Move();
            }
        }

        private void ChangeParent(ShooterTransformObserver newParent)
        {
            if (newParent == parent) return;

            if (parent != null) parent.children.Remove(this);
            parent = newParent;
            if (parent != null) parent.children.Add(this);
            ParentChanged?.Invoke();
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
