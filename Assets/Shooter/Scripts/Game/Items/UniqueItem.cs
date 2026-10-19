using System;
using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public abstract class UniqueItem : INetworkSerializable, ISaveable
    {
        protected UniqueItem(string assetId)
        {
            AssetId = assetId;
        }

        public string AssetId { get; private set; }

        public event Action Changed;

        public abstract void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter;

        public abstract object SaveObject();

        public abstract void LoadObject(SaveToken content);

        protected void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
