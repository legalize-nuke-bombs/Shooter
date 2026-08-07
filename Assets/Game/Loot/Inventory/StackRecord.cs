using System;
using Unity.Collections;
using Unity.Netcode;

namespace Shooter.Game.Loot
{
    public struct StackRecord : INetworkSerializable, IEquatable<StackRecord>
    {
        private FixedString32Bytes specId;
        private int amount;

        public StackRecord(FixedString32Bytes specId, int amount)
        {
            this.specId = specId;
            this.amount = amount;
        }

        public FixedString32Bytes SpecId => specId;

        public int Amount => amount;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref specId);
            serializer.SerializeValue(ref amount);
        }

        public bool Equals(StackRecord other)
        {
            return specId == other.specId && amount == other.amount;
        }
    }
}
