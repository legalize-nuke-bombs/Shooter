using Unity.Netcode;

namespace Shooter.Game
{
    public interface IUsable
    {
        UsageType Usage { get; }

        void Use(NetworkObject user, out string promptResult);
    }
}
