using Unity.Netcode;

namespace Shooter.Game
{
    public struct RadioPartner : INetworkSerializable
    {
        public long CharacterId;
        public bool OutOfReach;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref CharacterId);
            serializer.SerializeValue(ref OutOfReach);
        }
    }
}
