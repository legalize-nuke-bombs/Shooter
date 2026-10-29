using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public abstract class ServerPage : MainMenuPage
    {
        private const string CompressionDropdownName = "compression";
        private const string ProviderDropdownName = "provider";
        private const string EffortDropdownName = "effort";
        private const string NoProvider = "";
        private static readonly Journal Log = Logs.Here();

        protected ServerPage(VisualElement root) : base(root)
        {
            SetDropdownChoices(RequireElement<DropdownField>(CompressionDropdownName), MainCompressionManager.Keys, SettingsDropdownTitles.GetCompressionTitle);

            IEnumerable<string> providerNames = new[] { NoProvider }.Concat(LlmHosts.ProviderNames);
            SetDropdownChoices(RequireElement<DropdownField>(ProviderDropdownName), providerNames, SettingsDropdownTitles.GetProviderTitle);
            SetDropdownChoices(RequireElement<DropdownField>(EffortDropdownName), LlmEfforts.Keys, SettingsDropdownTitles.GetEffortTitle);
        }

        protected override void HandlePageHidden()
        {
            Config.SaveGameConfig();

            ServerConfig serverConfig = Config.GetGameConfig().Server;
            Log.Info($"Server settings: port {serverConfig.Port}, saves as '{serverConfig.SaveCompressionAlgorithm}', model {serverConfig.Llm.Provider}/{serverConfig.Llm.Model}, effort '{serverConfig.Llm.Effort}'");
        }
    }
}
