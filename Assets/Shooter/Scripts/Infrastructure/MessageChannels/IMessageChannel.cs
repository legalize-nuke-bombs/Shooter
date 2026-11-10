using Unity.Netcode;

namespace Shooter.Infrastructure
{
    public interface IMessageChannel
    {
        void Receive(FastBufferReader reader);
    }
}
