using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    public class ShooterTransformObserver : MonoBehaviour, IShooterLateUpdateable
    {
        [SerializeField] private TransformMobility mobility;

        private readonly List<ShooterTransformObserver> followers = new();
        private ShooterTransformObserver leader;
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

        private void Start()
        {
            started = true;
            lastPosition = transform.position;
            Placed?.Invoke();
            Follow(FindLeader());
        }

        private void OnTransformParentChanged()
        {
            if (!started || mobility == TransformMobility.Static) return;

            Follow(FindLeader());
            Move();
        }

        private void OnDestroy()
        {
            if (leader != null) leader.followers.Remove(this);
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
            foreach (ShooterTransformObserver follower in followers)
            {
                follower.Move();
            }
        }

        private void Follow(ShooterTransformObserver newLeader)
        {
            if (leader != null) leader.followers.Remove(this);

            leader = newLeader;
            if (leader != null) leader.followers.Add(this);
            enabled = mobility == TransformMobility.Dynamic && leader == null;
        }

        private ShooterTransformObserver FindLeader()
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
