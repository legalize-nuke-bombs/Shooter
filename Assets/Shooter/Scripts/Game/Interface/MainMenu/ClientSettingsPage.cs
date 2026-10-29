using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class ClientSettingsPage : MainMenuPage
    {
        private const string BackButtonName = "back";
        private const string ScreenResolutionDropdownName = "screen-resolution";
        private const string UpscalerDropdownName = "upscaler";
        private const string RenderScaleDropdownName = "render-scale";
        private const string AntialiasingDropdownName = "antialiasing";
        private const string FogDropdownName = "fog";
        private static readonly Journal Log = Logs.Here();

        private readonly DropdownField antialiasingDropdown;
        private readonly DropdownField renderScaleDropdown;
        private ClientConfig clientConfig;

        public ClientSettingsPage(VisualElement root) : base(root)
        {
            RequireElement<Button>(BackButtonName).clicked += () => BackClicked?.Invoke();
            SetDropdownChoices(RequireElement<DropdownField>(ScreenResolutionDropdownName), ScreenResolutions.GetKeysOfThisScreen(), SettingsDropdownTitles.GetScreenResolutionTitle);
            SetDropdownChoices(RequireElement<DropdownField>(UpscalerDropdownName), GetUpscalersOfThisCard(), SettingsDropdownTitles.GetUpscalerTitle);
            renderScaleDropdown = RequireElement<DropdownField>(RenderScaleDropdownName);
            SetDropdownChoices(renderScaleDropdown, RenderScales.Keys, SettingsDropdownTitles.GetRenderScaleTitle);
            antialiasingDropdown = RequireElement<DropdownField>(AntialiasingDropdownName);
            SetDropdownChoices(antialiasingDropdown, Antialiasings.Keys, SettingsDropdownTitles.GetAntialiasingTitle);
            SetDropdownChoices(RequireElement<DropdownField>(FogDropdownName), FogQualities.Keys, SettingsDropdownTitles.GetFogTitle);
        }

        public event Action BackClicked;

        protected override void HandlePageShown()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            EnableDropdownsThatApply();
        }

        private void HandleClientConfigChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            EnableDropdownsThatApply();
        }

        private void EnableDropdownsThatApply()
        {
            bool upscaling = clientConfig.Upscaler != Upscalers.Off;
            renderScaleDropdown.SetEnabled(upscaling);
            antialiasingDropdown.SetEnabled(!upscaling);
        }

        private static IEnumerable<string> GetUpscalersOfThisCard()
        {
            return HDDynamicResolutionPlatformCapabilities.DLSSDetected ? Upscalers.Keys : Upscalers.Keys.Where(key => key != Upscalers.Dlss);
        }

        protected override void HandlePageHidden()
        {
            if (clientConfig != null) clientConfig.propertyChanged -= HandleClientConfigChanged;
            Config.SaveGameConfig();

            ClientConfig savedClientConfig = Config.GetGameConfig().Client;
            Log.Info($"Client settings: master {savedClientConfig.Master:0.00}, music {savedClientConfig.Music:0.00}, ambience {savedClientConfig.Ambience:0.00}, sounds {savedClientConfig.Sounds:0.00}, screen {savedClientConfig.ScreenResolution}, upscaler {savedClientConfig.Upscaler}, render scale {savedClientConfig.RenderScale}%, antialiasing {savedClientConfig.Antialiasing}, fog {savedClientConfig.Fog}");
        }
    }
}
