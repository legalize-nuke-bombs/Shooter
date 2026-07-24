using Shooter.Client;

namespace Shooter.Server.Protocol
{
    public abstract class ClientBound
    {
        public abstract void Apply(ClientHost host);
    }
}
