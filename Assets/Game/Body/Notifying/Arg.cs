using Unity.Netcode;

namespace Shooter.Game.Body.Notifying
{
    public struct Arg : INetworkSerializable
    {
        private string name;
        private string value;

        public Arg(string name, string value)
        {
            this.name = name ?? string.Empty;
            this.value = value ?? string.Empty;
        }

        public string Name => name ?? string.Empty;

        public string Value => value ?? string.Empty;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                name ??= string.Empty;
                value ??= string.Empty;
            }

            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref value);
        }
    }
}
