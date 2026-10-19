using System;
using Newtonsoft.Json;

namespace Shooter.Infrastructure
{
    public class SaveTokenJsonConverter : JsonConverter<SaveToken>
    {
        public override void WriteJson(JsonWriter writer, SaveToken value, JsonSerializer serializer)
        {
            if (value.IsEmpty)
            {
                writer.WriteNull();
                return;
            }

            value.WriteToJson(writer);
        }

        public override SaveToken ReadJson(JsonReader reader, Type objectType, SaveToken existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return SaveToken.ReadFromJson(reader);
        }
    }
}
