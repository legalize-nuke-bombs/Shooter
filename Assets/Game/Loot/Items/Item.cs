using System;
using Unity.Netcode;

namespace Shooter.Game.Items
{
    [Serializable]
    public struct Item : INetworkSerializable, IEquatable<Item>
    {
        public ItemType Type;
        public int Amount;
        public int Magazine;

        public Item(ItemType type, int amount, int magazine)
        {
            Type = type;
            Amount = amount;
            Magazine = magazine;
        }

        public bool Empty => Amount <= 0;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Type);
            serializer.SerializeValue(ref Amount);
            serializer.SerializeValue(ref Magazine);
        }

        public bool Equals(Item other)
        {
            return Type == other.Type && Amount == other.Amount && Magazine == other.Magazine;
        }
    }
}
