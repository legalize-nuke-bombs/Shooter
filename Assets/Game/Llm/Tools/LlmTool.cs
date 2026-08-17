using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Llm
{
    public abstract class LlmTool : MonoBehaviour, ILlmTool
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract JObject Parameters { get; }

        public virtual bool Available => true;
        public virtual LlmLevel Level => LlmLevel.Base;

        public abstract string Execute(string arguments);
    }

    public abstract class LlmTool<TArguments> : LlmTool
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        };

        private JObject parameters;
        public override JObject Parameters => parameters ??= LlmSchema.Of(typeof(TArguments));

        protected virtual void Awake() { }

        public override string Execute(string arguments)
        {
            var parsed = JsonConvert.DeserializeObject<TArguments>(
                string.IsNullOrEmpty(arguments) ? "{}" : arguments, Settings);

            string result = Execute(parsed);
            Log.Info($"Entity {name} used {Name} {arguments}: {result}");

            return result;
        }

        protected abstract string Execute(TArguments arguments);
    }
}
