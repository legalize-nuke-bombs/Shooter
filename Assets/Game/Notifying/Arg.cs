using Shooter.Game.Body;
using Shooter.Game.Loot;
using Unity.Netcode;
using Environment = Shooter.Game.World.Environment;
using Shooter.Game.Core;

namespace Shooter.Game.Notifying
{
    public struct Arg : INetworkSerializable
    {
        private string name;
        private string value;
        private byte type;

        public Arg(string name, string value, ArgType type = ArgType.Raw)
        {
            this.name = name ?? string.Empty;
            this.value = value ?? string.Empty;
            this.type = (byte)type;
        }

        public string Name => name ?? string.Empty;

        public string Value => value ?? string.Empty;

        public ArgType Type => (ArgType)type;

        public string Rendered()
        {
            switch (Type)
            {
                case ArgType.Name:
                    return Named(false);
                case ArgType.NamePrompt:
                    return Named(true);
                case ArgType.Item:
                    return Titled(false);
                case ArgType.ItemPrompt:
                    return Titled(true);
                default:
                    return Value;
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                name ??= string.Empty;
                value ??= string.Empty;
            }

            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref value);
            serializer.SerializeValue(ref type);
        }

        private string Named(bool prompted)
        {
            NameCatalog catalog = Catalogs.Of<NameCatalog>();
            NameSpec spec = catalog == null ? null : catalog.Of(Value);

            if (spec == null) return Value;

            return prompted ? spec.Prompt() : spec.Text();
        }

        private string Titled(bool prompted)
        {
            ItemCatalog catalog = Catalogs.Of<ItemCatalog>();
            ItemSpec spec = catalog == null ? null : catalog.Of(Value);

            if (spec == null) return Value;

            return prompted ? spec.Id.ToString() : spec.Title;
        }
    }
}
