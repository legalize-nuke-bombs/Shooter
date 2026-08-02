using System;
using Shooter.Configuring;
using Shooter.Game.Llm.Polza;

namespace Shooter.Game.Llm
{
    public static class LlmApiProviders
    {
        public static ILlmApiProvider For(LlmConfig config)
        {
            switch (config.Provider.ToLower())
            {
                case "polza":
                    return new PolzaApiProvider();
                default:
                    throw new InvalidOperationException($"No llm api provider registered for '{config.Provider}'");
            }
        }
    }
}
