using Unity.Netcode;

namespace Shooter.Game.Core
{
    public struct Packed<TBase> : INetworkSerializable where TBase : class, INetworkSerializable
    {
        private TBase value;

        public Packed(TBase value)
        {
            this.value = value;
        }

        public TBase Value => value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            IKinds<TBase> kinds = Kinds.Of<TBase>();

            int kind = serializer.IsWriter ? kinds.Of(value) : 0;
            serializer.SerializeValue(ref kind);

            if (serializer.IsReader) value = kinds.Create(kind);
            if (value == null) return;

            value.NetworkSerialize(serializer);
        }
    }
}
