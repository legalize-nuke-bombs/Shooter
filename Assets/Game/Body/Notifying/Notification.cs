using Unity.Netcode;

namespace Shooter.Game.Body.Notifying
{
    public abstract class Notification : INetworkSerializable
    {
        public abstract void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter;
    }
}
