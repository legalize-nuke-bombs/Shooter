using System;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    public class TransformObserver : MonoBehaviour, IShooterLateUpdateable
    {
        private Vector3 lastPosition;

        public event Action Moved;

        private void Awake()
        {
            lastPosition = transform.position;
        }

        public void ShooterLateUpdate()
        {
            Vector3 position = transform.position;
            if (position == lastPosition) return;

            lastPosition = position;
            Moved?.Invoke();
        }
    }
}
