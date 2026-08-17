using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Shooter.Game.Llm
{
    public static class LlmSchema
    {
        private static readonly SnakeCaseNamingStrategy Snake = new SnakeCaseNamingStrategy();

        public static JObject Of(Type arguments)
        {
            var properties = new JObject();
            var required = new JArray();

            foreach (PropertyInfo property in arguments.GetProperties())
            {
                string name = Snake.GetPropertyName(property.Name, false);

                properties[name] = Typed(property.PropertyType);
                required.Add(name);
            }

            var schema = new JObject { ["type"] = "object", ["properties"] = properties };
            if (required.Count > 0) schema["required"] = required;

            return schema;
        }

        private static JObject Typed(Type type)
        {
            if (type.IsArray) return new JObject { ["type"] = "array", ["items"] = Typed(type.GetElementType()) };
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return new JObject { ["type"] = "object", ["additionalProperties"] = Typed(type.GetGenericArguments()[1]) };
            if (type == typeof(string)) return new JObject { ["type"] = "string" };
            if (type == typeof(bool)) return new JObject { ["type"] = "boolean" };
            if (type == typeof(float) || type == typeof(double)) return new JObject { ["type"] = "number" };

            return new JObject { ["type"] = "integer" };
        }
    }
}
