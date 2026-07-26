using Unity.Netcode;

namespace Shooter.Game.Interacting
{
    public interface IUsable
    {
        UsageType Usage { get; }

        void Use(NetworkObject user);
    }
}
