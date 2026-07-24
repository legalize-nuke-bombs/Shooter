using Shooter.Server.Sessions;

namespace Shooter.Server.Protocol
{
    public class JoinWorld : ServerBound
    {
        public override void Apply(ServerHost host, ServerSession session)
        {
            host.EnterWorld(session);
        }
    }
}
