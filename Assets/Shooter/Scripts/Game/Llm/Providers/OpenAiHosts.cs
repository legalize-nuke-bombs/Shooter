using System;
using System.Collections.Generic;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public static class OpenAiHosts
    {
        private static readonly Dictionary<string, Func<IOpenAiHost>> HostFactoriesByProvider = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Polza"] = () => new PolzaHost(),
            ["Ollama"] = () => new OllamaHost()
        };

        public static IEnumerable<string> ProviderNames => HostFactoriesByProvider.Keys;

        public static IOpenAiHost CreateHostFor(LlmConfig config)
        {
            if (HostFactoriesByProvider.TryGetValue(config.Provider, out Func<IOpenAiHost> createHost)) return createHost();

            throw new InvalidOperationException($"No llm host registered for '{config.Provider}'");
        }
    }
}
