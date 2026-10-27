using Unity.Netcode;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public abstract class ShooterNetworkBehaviour : NetworkBehaviour, IShooterBehaviour
    {
        public MonoBehaviour NativeBehaviour => this;

        protected void Awake()
        {
            Registers.Instance.RegisterBehaviour(this);
            ShooterAwake();
        }

        protected void OnEnable()
        {
            Registers.Instance.ActivateBehaviour(this);
            ShooterOnEnable();
        }

        protected void OnDisable()
        {
            ShooterOnDisable();
            Registers.Instance.DeactivateBehaviour(this);
        }

        public sealed override void OnDestroy()
        {
            ShooterOnDestroy();
            Registers.Instance.UnregisterBehaviour(this);
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
