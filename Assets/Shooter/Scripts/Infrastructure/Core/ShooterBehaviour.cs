using UnityEngine;

namespace Shooter.Infrastructure
{
    public abstract class ShooterBehaviour : MonoBehaviour, IShooterBehaviour
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

        protected void OnDestroy()
        {
            ShooterOnDestroy();
            Registers.Instance.UnregisterBehaviour(this);
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
