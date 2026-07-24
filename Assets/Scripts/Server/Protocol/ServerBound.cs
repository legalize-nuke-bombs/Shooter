using Shooter.Server.Sessions;

namespace Shooter.Server.Protocol
{
    public abstract class ServerBound
    {
        public abstract void Apply(ServerHost host, ServerSession session);
    }
}
