using System;
using Newtonsoft.Json.Linq;
using Shooter.Game.Llm.OpenAi;

namespace Shooter.Game.Llm
{
    public sealed class LlmTool
    {
        public string Name { get; }
        public string Description { get; }
        public JObject Parameters { get; }
        public Func<JObject, string> Execute { get; }

        public LlmTool(string name, string description, string parameters, Func<JObject, string> execute)
        {
            Name = name;
            Description = description;
            Parameters = JObject.Parse(parameters);
            Execute = execute;
        }

        public OpenAiTool Declared()
        {
            return new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = Name,
                    Description = Description,
                    Parameters = Parameters
                }
            };
        }
    }
}
