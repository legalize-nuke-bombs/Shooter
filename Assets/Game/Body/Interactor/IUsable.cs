using Unity.Netcode;

namespace Shooter.Game.Body
{
    public interface IUsable
    {
        UsageType Usage { get; }

        void Use(NetworkObject user);
    }
}
