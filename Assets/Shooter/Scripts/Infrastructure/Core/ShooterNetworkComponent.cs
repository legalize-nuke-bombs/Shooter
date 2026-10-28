using Unity.Netcode;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [RequireComponent(typeof(ShooterEntity))]
    public abstract class ShooterNetworkComponent : NetworkBehaviour
    {
        private protected ShooterEntity Entity { get; private set; }

        protected void Awake()
        {
            Entity = GetComponent<ShooterEntity>();
            Entity.NotifyComponentAttached(this);
            ShooterAwake();
        }

        protected void OnEnable()
        {
            Entity.NotifyComponentEnabled(this);
            ShooterOnEnable();
        }

        protected void OnDisable()
        {
            ShooterOnDisable();
            Entity.NotifyComponentDisabled(this);
        }

        public sealed override void OnDestroy()
        {
            ShooterOnDestroy();
            Entity.NotifyComponentDetached(this);
            base.OnDestroy();
        }

        protected virtual void ShooterAwake()
        {
        }

        protected virtual void ShooterOnEnable()
        {
        }

        protected virtual void ShooterOnDisable()
        {
        }

        protected virtual void ShooterOnDestroy()
        {
        }
    }
}
