using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Shooter.Logging;

namespace Shooter.Serialization
{
    public static class Json
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = new OwnTypesBinder(),
            Converters = { new StringEnumConverter() }
        };

        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }

        public static string Serialize(object value, Type declaredType)
        {
            return JsonConvert.SerializeObject(value, declaredType, Settings);
        }

        public static T Deserialize<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, Settings);
            }
            catch (JsonException e)
            {
                Log.Warn("Failed to read json as {}: {}", typeof(T).Name, e.Message);
                return default;
            }
        }

        private sealed class OwnTypesBinder : DefaultSerializationBinder
        {
            private const string OwnNamespace = "Shooter.";

            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName == null || !typeName.StartsWith(OwnNamespace, StringComparison.Ordinal))
                    throw new JsonSerializationException("Type " + typeName + " is not allowed on the wire");

                return base.BindToType(assemblyName, typeName);
            }
        }
    }
}
