using Unity.Netcode;

namespace Shooter.Game.Loot
{
    public class UniqueItem : INetworkSerializable
    {
        public string SpecId { get; private set; }

        public bool Dirty { get; private set; }

        public UniqueItem(string specId)
        {
            SpecId = specId;
        }

        public void Clean()
        {
            Dirty = false;
        }

        public virtual void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
        }

        protected void Touch()
        {
            Dirty = true;
        }
    }
}
