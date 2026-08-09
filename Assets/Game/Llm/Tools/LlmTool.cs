using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shooter.Game.Base;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.Tools
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

        private LlmToolProfiler profiler;

        protected virtual void Awake()
        {
            profiler = Environment.Current.Profiler.GetComponent<LlmToolProfiler>();
            if (profiler == null)
            {
                Log.Error("Failed to find LlmToolProfiler!");
            }
        }

        public override string Execute(string arguments)
        {
            profiler?.RegisterTool(GetType().Name);

            var parsed = JsonConvert.DeserializeObject<TArguments>(
                string.IsNullOrEmpty(arguments) ? "{}" : arguments, Settings);

            string result = Execute(parsed);
            Log.Info($"Entity {name} used {Name} {arguments}: {result}");

            return result;
        }

        protected abstract string Execute(TArguments arguments);
    }
}
