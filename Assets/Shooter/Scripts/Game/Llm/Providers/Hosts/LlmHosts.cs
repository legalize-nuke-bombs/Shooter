using System;
using System.Collections.Generic;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public static class LlmHosts
    {
        private static readonly Dictionary<string, Func<ILlmHost>> HostFactoriesByProvider = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Polza"] = () => new PolzaHost(),
            ["Ollama"] = () => new OllamaHost(),
            ["Claude"] = () => new ClaudeHost()
        };

        public static IEnumerable<string> ProviderNames => HostFactoriesByProvider.Keys;

        public static ILlmHost CreateHostFor(string provider)
        {
            if (HostFactoriesByProvider.TryGetValue(provider, out Func<ILlmHost> createHost)) return createHost();

            throw new InvalidOperationException($"No llm host registered for '{provider}'");
        }
    }
}
