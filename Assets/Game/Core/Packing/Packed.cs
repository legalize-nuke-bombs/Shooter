using Unity.Netcode;

namespace Shooter.Game.Core
{
    public struct Packed<TBase> : INetworkSerializable where TBase : class, INetworkSerializable
    {
        public Packed(TBase value)
        {
            this.Value = value;
        }

        public TBase Value { get; private set; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            IKinds<TBase> kinds = Kinds.Of<TBase>();

            int kind = serializer.IsWriter ? kinds.Of(Value) : 0;
            serializer.SerializeValue(ref kind);

            if (serializer.IsReader) Value = kinds.Create(kind);
            if (Value == null) return;

            Value.NetworkSerialize(serializer);
        }
    }
}
