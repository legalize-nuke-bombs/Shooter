using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public class Artifact : UniqueItem
    {
        public Artifact(string assetId) : base(assetId)
        {
        }

        private struct SaveData
        {

        }
        public override object SaveObject()
        {
            return new SaveData {  };
        }
        public override void LoadObject(SaveToken content)
        {

        }

        public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
        {

        }
    }
}
