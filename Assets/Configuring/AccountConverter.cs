using System;
using Newtonsoft.Json;
using Shooter.Accounts;

namespace Shooter.Configuring
{
    public class AccountConverter : JsonConverter<Account>
    {
        public override void WriteJson(JsonWriter writer, Account value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.Key);
        }

        public override Account ReadJson(JsonReader reader, Type objectType, Account existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var secret = reader.Value as string;
            return string.IsNullOrEmpty(secret) ? null : Account.FromKey(secret);
        }
    }
}
