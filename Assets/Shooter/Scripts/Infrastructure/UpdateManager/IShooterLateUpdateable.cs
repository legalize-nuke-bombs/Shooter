namespace Shooter.Infrastructure
{
    public interface IShooterLateUpdateable : IShooterBehaviour
    {
        void ShooterLateUpdate();
    }
}
