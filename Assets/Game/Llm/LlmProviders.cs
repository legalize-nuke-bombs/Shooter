using System;
using Shooter.Configuring;
using Shooter.Game.Llm.Gemini;

namespace Shooter.Game.Llm
{
    public static class LlmProviders
    {
        public static ILlmProvider For(LlmConfig config)
        {
            if (config.Model.StartsWith("gemini", StringComparison.Ordinal)) return new GeminiProvider();

            throw new InvalidOperationException($"No llm provider serves model {config.Model}");
        }
    }
}
