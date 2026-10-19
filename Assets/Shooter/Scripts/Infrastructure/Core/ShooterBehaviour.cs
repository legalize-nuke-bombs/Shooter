using UnityEngine;

namespace Shooter.Infrastructure
{
    public abstract class ShooterBehaviour : MonoBehaviour, IShooterBehaviour
    {
        protected void Awake()
        {
            Registers.Add(this);
            ShooterAwake();
        }

        protected void OnDestroy()
        {
            ShooterOnDestroy();
            Registers.Remove(this);
        }

        protected virtual void ShooterAwake()
        {
        }

        protected virtual void ShooterOnDestroy()
        {
        }
    }
}
