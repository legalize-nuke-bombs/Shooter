using Unity.Netcode;

namespace Shooter.Game.Interacting
{
    public interface IUsable
    {
        void Use(NetworkObject user);
    }
}
