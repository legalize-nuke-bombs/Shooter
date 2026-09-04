using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [Serializable]
    public abstract class LlmTool : ILlmTool
    {
        protected GameObject Self { get; private set; }

        public virtual bool Available => true;
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract JObject Parameters { get; }

        public void Attach(GameObject self)
        {
            Self = self;
            OnStart();
        }

        protected abstract void OnStart();

        public abstract string Execute(string arguments, LlmCallContext context);
    }

    [Serializable]
    public abstract class LlmTool<TArguments> : LlmTool
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        };

        private JObject parameters;
        public override JObject Parameters => parameters ??= LlmSchema.Of(typeof(TArguments));

        public override string Execute(string arguments, LlmCallContext context)
        {
            TArguments parsed = JsonConvert.DeserializeObject<TArguments>(
                string.IsNullOrEmpty(arguments) ? "{}" : arguments, Settings);

            string result = Execute(parsed, context);
            Log.Info($"Entity {Self.name} used {Name} {arguments}: {result}");

            return result;
        }

        protected abstract string Execute(TArguments arguments, LlmCallContext context);
    }
}
