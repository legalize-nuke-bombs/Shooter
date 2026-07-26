using System;
using Unity.Collections;
using Unity.Netcode;

namespace Shooter.Game.Loot
{
    public struct Item : INetworkSerializable, IEquatable<Item>
    {
        public FixedString32Bytes Id;
        public int Amount;
        public int State;

        public Item(FixedString32Bytes id, int amount, int state)
        {
            Id = id;
            Amount = amount;
            State = state;
        }

        public bool Empty => Amount <= 0;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Id);
            serializer.SerializeValue(ref Amount);
            serializer.SerializeValue(ref State);
        }

        public bool Equals(Item other)
        {
            return Id == other.Id && Amount == other.Amount && State == other.State;
        }
    }
}
