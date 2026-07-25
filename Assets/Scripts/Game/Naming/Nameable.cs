using Unity.Netcode;

namespace Shooter.Game.Naming
{
    public abstract class Nameable : NetworkBehaviour, IDigestible
    {
        public abstract string Digest();
    }
}
