using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public abstract class LlmTool : ILlmTool
    {
        protected GameObject Self { get; private set; }

        public virtual bool IsAvailable => true;
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract JObject Parameters { get; }

        public void AttachTo(GameObject owner)
        {
            Self = owner;
            Start();
        }

        protected abstract void Start();

        public abstract string Execute(string arguments, LlmCallContext context);
    }

    [Serializable]
    public abstract class LlmTool<TArguments> : LlmTool
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly JsonSerializerSettings ArgumentsJsonSettings = new()
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        };

        private JObject parametersSchema;
        public override JObject Parameters => parametersSchema ??= LlmSchema.BuildObjectSchema(typeof(TArguments));

        public override string Execute(string arguments, LlmCallContext context)
        {
            TArguments parsedArguments = JsonConvert.DeserializeObject<TArguments>(string.IsNullOrEmpty(arguments) ? "{}" : arguments, ArgumentsJsonSettings);

            string result = Execute(parsedArguments, context);
            Log.Info($"Entity {Self.name} used {Name} {arguments}: {result}");

            return result;
        }

        protected abstract string Execute(TArguments arguments, LlmCallContext context);
    }
}
