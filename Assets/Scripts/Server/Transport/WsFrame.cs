namespace Shooter.Server.Transport
{
    public struct WsFrame
    {
        public bool Final;
        public int Opcode;
        public byte[] Payload;
    }
}
