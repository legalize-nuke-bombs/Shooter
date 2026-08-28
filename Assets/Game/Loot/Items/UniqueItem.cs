using Shooter.Game.Core.Saves;
using Unity.Netcode;

namespace Shooter.Game.Loot
{
    public class UniqueItem : INetworkSerializable, ISaveable
    {
        public UniqueItem(string specId)
        {
            SpecId = specId;
        }

        public string SpecId { get; private set; }

        public bool Dirty { get; private set; }

        public virtual void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
        }

        public virtual object SaveObject()
        {
            return null;
        }

        public virtual void LoadObject(SaveToken content)
        {
        }

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
