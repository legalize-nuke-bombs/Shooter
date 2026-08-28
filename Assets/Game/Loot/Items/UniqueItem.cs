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

        public bool Dirty { get; private set; }

        public abstract void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter;

        public abstract object SaveObject();

        public abstract void LoadObject(SaveToken content);

        public void Clean()
        {
            Dirty = false;
        }

        protected void Touch()
        {
            Dirty = true;
        }
    }
}
