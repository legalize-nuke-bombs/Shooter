namespace Shooter.Infrastructure
{
    public interface IWantsEnabledHandler
    {
        void OnWantsEnabled();

        void OnWantsDisabled();
    }
}
