using UnityEngine;

namespace Shooter.Infrastructure
{
    public abstract class ShooterBehaviour : MonoBehaviour, IShooterBehaviour
    {
        public MonoBehaviour NativeBehaviour => this;

        protected void Awake()
        {
            Registers.RegisterBehaviour(this);
            ShooterAwake();
        }

        protected void OnEnable()
        {
            Registers.ActivateBehaviour(this);
            ShooterOnEnable();
        }

        protected void OnDisable()
        {
            ShooterOnDisable();
            Registers.DeactivateBehaviour(this);
        }

        protected void OnDestroy()
        {
            ShooterOnDestroy();
            Registers.UnregisterBehaviour(this);
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
