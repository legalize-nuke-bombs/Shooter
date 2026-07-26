using Unity.Netcode;

namespace Shooter.Game.Body
{
    public abstract class Nameable : NetworkBehaviour, IDigestible
    {
        public abstract string Digest();
    }
}
