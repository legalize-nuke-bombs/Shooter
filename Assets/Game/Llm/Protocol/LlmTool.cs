using System;
using Newtonsoft.Json.Linq;

namespace Shooter.Game.Llm
{
    public sealed class LlmTool
    {
        public string Name { get; }
        public string Description { get; }
        public JObject Parameters { get; }
        public Func<JObject, string> Execute { get; }

        public LlmTool(string name, string description, JObject parameters, Func<JObject, string> execute)
        {
            Name = name;
            Description = description;
            Parameters = parameters;
            Execute = execute;
        }

        public static JObject Schema(params (string name, string type, string description)[] fields)
        {
            var properties = new JObject();
            var required = new JArray();

            foreach ((string name, string type, string description) field in fields)
            {
                JObject property = field.type.EndsWith("[]")
                    ? new JObject { ["type"] = "array", ["items"] = new JObject { ["type"] = field.type[..^2] } }
                    : new JObject { ["type"] = field.type };

                if (!string.IsNullOrEmpty(field.description)) property["description"] = field.description;

                properties[field.name] = property;
                required.Add(field.name);
            }

            var schema = new JObject { ["type"] = "object", ["properties"] = properties };
            if (required.Count > 0) schema["required"] = required;

            return schema;
        }
    }
}
