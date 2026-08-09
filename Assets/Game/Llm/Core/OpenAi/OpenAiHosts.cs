using System;
using Shooter.Configuring;
using Shooter.Game.Llm.OpenAi.Polza;

namespace Shooter.Game.Llm.OpenAi
{
    public static class OpenAiHosts
    {
        public static IOpenAiHost For(LlmConfig config)
        {
            switch (config.Provider.ToLower())
            {
                case "polza":
                    return new PolzaHost();
                default:
                    throw new InvalidOperationException($"No llm host registered for '{config.Provider}'");
            }
        }
    }
}
