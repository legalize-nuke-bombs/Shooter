using UnityEngine;

namespace Shooter.Infrastructure
{
    public abstract class ShooterBehaviour : MonoBehaviour, IShooterBehaviour
    {
        protected void Awake()
        {
            Registers.RegisterBehaviour(this);
            ShooterAwake();
        }

        protected void OnDestroy()
        {
            ShooterOnDestroy();
            Registers.UnregisterBehaviour(this);
        }

        protected virtual void ShooterAwake()
        {
        }

        protected virtual void ShooterOnDestroy()
        {
        }
    }
}
