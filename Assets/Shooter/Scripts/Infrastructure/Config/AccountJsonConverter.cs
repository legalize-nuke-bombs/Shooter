using System;
using Newtonsoft.Json;

namespace Shooter.Infrastructure
{
    public class AccountJsonConverter : JsonConverter<Account>
    {
        private static readonly Journal Log = Logs.Here();

        public override void WriteJson(JsonWriter writer, Account value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.PrivateKey);
        }

        public override Account ReadJson(JsonReader reader, Type objectType, Account existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string privateKey = reader.Value as string;
            if (string.IsNullOrEmpty(privateKey)) return null;

            try
            {
                return Account.FromPrivateKey(privateKey);
            }
            catch (Exception exception)
            {
                Log.Warn($"The account key in the config is unreadable, the account is left empty: {exception.Message}");
                return null;
            }
        }
    }
}
