using System;
using System.Collections.Generic;
using Shooter.Configuring;

namespace Shooter.Game.Llm
{
    public static class OpenAiHosts
    {
        private static readonly Dictionary<string, Func<IOpenAiHost>> Hosts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Polza"] = () => new PolzaHost(),
            ["Ollama"] = () => new OllamaHost()
        };

        public static IEnumerable<string> Providers => Hosts.Keys;

        public static IOpenAiHost For(LlmConfig config)
        {
            if (Hosts.TryGetValue(config.Provider, out Func<IOpenAiHost> host)) return host();

            throw new InvalidOperationException($"No llm host registered for '{config.Provider}'");
        }
    }
}
