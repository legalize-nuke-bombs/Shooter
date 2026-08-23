using System;
using Newtonsoft.Json;

namespace Shooter.Game.Core.Saves
{
    public class SaveTokenConverter : JsonConverter<SaveToken>
    {
        public override void WriteJson(JsonWriter writer, SaveToken value, JsonSerializer serializer)
        {
            value.Write(writer);
        }

        public override SaveToken ReadJson(JsonReader reader, Type objectType, SaveToken existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            return SaveToken.Read(reader);
        }
    }
}
