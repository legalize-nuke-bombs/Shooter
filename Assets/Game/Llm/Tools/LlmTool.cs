using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Shooter.Game.Llm.Tools
{
    public abstract class LlmTool : MonoBehaviour, ILlmTool
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract JObject Parameters { get; }

        public virtual bool Available => true;

        public abstract string Execute(string arguments);
    }

    public abstract class LlmTool<TArguments> : LlmTool
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        };

        private JObject parameters;
        public override JObject Parameters => parameters ??= LlmSchema.Of(typeof(TArguments));

        public override string Execute(string arguments)
        {
            var parsed = JsonConvert.DeserializeObject<TArguments>(
                string.IsNullOrEmpty(arguments) ? "{}" : arguments, Settings);

            return Execute(parsed);
        }

        protected abstract string Execute(TArguments arguments);
    }
}
