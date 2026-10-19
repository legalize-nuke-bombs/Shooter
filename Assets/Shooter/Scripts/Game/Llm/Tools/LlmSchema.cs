using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Shooter.Game
{
    public static class LlmSchema
    {
        private static readonly SnakeCaseNamingStrategy SnakeCaseNaming = new();

        public static JObject BuildObjectSchema(Type objectType)
        {
            JObject propertySchemas = new();
            JArray requiredPropertyNames = new();

            foreach (PropertyInfo property in objectType.GetProperties())
            {
                string propertyName = SnakeCaseNaming.GetPropertyName(property.Name, false);

                propertySchemas[propertyName] = BuildTypeSchema(property.PropertyType);
                requiredPropertyNames.Add(propertyName);
            }

            JObject schema = new() { ["type"] = "object", ["properties"] = propertySchemas };
            if (requiredPropertyNames.Count > 0) schema["required"] = requiredPropertyNames;

            return schema;
        }

        private static JObject BuildTypeSchema(Type type)
        {
            if (type.IsArray) return new JObject { ["type"] = "array", ["items"] = BuildTypeSchema(type.GetElementType()) };
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return new JObject { ["type"] = "object", ["additionalProperties"] = BuildTypeSchema(type.GetGenericArguments()[1]) };
            }
            if (type == typeof(string)) return new JObject { ["type"] = "string" };
            if (type == typeof(bool)) return new JObject { ["type"] = "boolean" };
            if (type == typeof(float) || type == typeof(double)) return new JObject { ["type"] = "number" };
            if (type.IsClass) return BuildObjectSchema(type);

            return new JObject { ["type"] = "integer" };
        }
    }
}
