using Unity.Netcode;
using Shooter.Game.Digesting;

namespace Shooter.Game.Naming
{
    public abstract class Nameable : NetworkBehaviour, IDigestible
    {
        public abstract string Digest();
    }
}
