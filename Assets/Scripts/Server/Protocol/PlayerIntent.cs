using Shooter.Server.Sessions;

namespace Shooter.Server.Protocol
{
    public class PlayerIntent : ServerBound
    {
        public float MoveX { get; set; }
        public float MoveZ { get; set; }
        public bool Sprint { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }

        public bool Jump { get; set; }
        public bool Use { get; set; }
        public bool Shoot { get; set; }
        public bool Reload { get; set; }
        public string Speech { get; set; }

        public override void Apply(ServerHost host, ServerSession session)
        {
            host.ApplyInput(session, this);
        }
    }
}
