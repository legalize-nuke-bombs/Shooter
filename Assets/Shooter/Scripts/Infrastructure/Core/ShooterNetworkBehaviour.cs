using Unity.Netcode;

namespace Shooter.Infrastructure
{
    public abstract class ShooterNetworkBehaviour : NetworkBehaviour, IShooterBehaviour
    {
        protected void Awake()
        {
            Registers.Add(this);
            ShooterAwake();
        }

        public sealed override void OnDestroy()
        {
            ShooterOnDestroy();
            Registers.Remove(this);
            base.OnDestroy();
        }

        protected virtual void ShooterAwake()
        {
        }

        protected virtual void ShooterOnDestroy()
        {
        }
    }
}
