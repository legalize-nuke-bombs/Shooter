using Newtonsoft.Json;

namespace Shooter.Game.Core.Saves
{
    public static class SaveJson
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            Converters = { new Vector3Converter(), new QuaternionConverter(), new SaveableConverter() }
        };

        public static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);
    }
}
