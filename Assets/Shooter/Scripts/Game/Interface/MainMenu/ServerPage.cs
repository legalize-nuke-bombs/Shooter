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
        private const string EffortDropdownName = nameof(LlmConfig.Effort);
        private const string LlmSettingClass = "llm-setting";
        private const string NoProvider = "";
        private static readonly Journal Log = Logs.Here();

        protected ServerPage(VisualElement root) : base(root)
        {
            SetDropdownChoices(RequireElement<DropdownField>(CompressionDropdownName), MainCompressionManager.Keys, SettingsDropdownTitles.GetCompressionTitle);

            IEnumerable<string> providerNames = new[] { NoProvider }.Concat(LlmHosts.ProviderNames);
            DropdownField providerDropdown = RequireElement<DropdownField>(ProviderDropdownName);
            SetDropdownChoices(providerDropdown, providerNames, SettingsDropdownTitles.GetProviderTitle);
            providerDropdown.RegisterValueChangedCallback(changeEvent => ShowLlmSettingsOf(changeEvent.newValue));
            SetDropdownChoices(RequireElement<DropdownField>(EffortDropdownName), LlmEfforts.Keys, SettingsDropdownTitles.GetEffortTitle);
        }

        protected override void HandlePageShown()
        {
            ShowLlmSettingsOf(Config.GetGameConfig().Server.Llm.Provider);
        }

        protected override void HandlePageHidden()
        {
            Config.SaveGameConfig();

            ServerConfig serverConfig = Config.GetGameConfig().Server;
            Log.Info($"Server settings: port {serverConfig.Port}, saves as '{serverConfig.SaveCompressionAlgorithm}', model {serverConfig.Llm.Provider}/{serverConfig.Llm.Model}, effort '{serverConfig.Llm.Effort}'");
        }

        private void ShowLlmSettingsOf(string provider)
        {
            IReadOnlyCollection<string> settings = provider == NoProvider ? Array.Empty<string>() : LlmHosts.CreateHostFor(provider).Settings;

            Root.Query(className: LlmSettingClass).ForEach(field => field.style.display = settings.Contains(field.name) ? DisplayStyle.Flex : DisplayStyle.None);
        }
    }
}
