using System;
using Shooter.Game.Core.Saves;
using Unity.Netcode;

namespace Shooter.Game.Loot
{
    public abstract class UniqueItem : INetworkSerializable, ISaveable
    {
        protected UniqueItem(string specId)
        {
            SpecId = specId;
        }

        public string SpecId { get; private set; }

        public event Action Changed;

        public abstract void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter;

        public abstract object SaveObject();

        public abstract void LoadObject(SaveToken content);

        protected void OnChanged()
        {
            Changed?.Invoke();
        }
    }
}
