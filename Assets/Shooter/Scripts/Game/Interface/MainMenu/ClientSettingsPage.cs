using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;
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
        private const string PathTracingDropdownName = "path-tracing";
        private static readonly Journal Log = Logs.Here();

        private static readonly string[] RayTracingModeDropdownNames =
        {
            "ray-traced-global-illumination",
            "ray-traced-reflections"
        };

        private static readonly string[] RayTracingToggleDropdownNames =
        {
            "ray-traced-ambient-occlusion",
            "ray-traced-sun-shadows",
            "ray-traced-lamp-shadows",
            "ray-traced-subsurface-scattering",
            "recursive-rendering"
        };

        private readonly DropdownField antialiasingDropdown;
        private readonly DropdownField renderScaleDropdown;
        private readonly DropdownField pathTracingDropdown;
        private readonly List<DropdownField> rayTracingEffectDropdowns = new();
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

            foreach (string name in RayTracingModeDropdownNames)
            {
                DropdownField dropdown = RequireElement<DropdownField>(name);
                SetDropdownChoices(dropdown, RayTracingModes.Keys, SettingsDropdownTitles.GetRayTracingModeTitle);
                rayTracingEffectDropdowns.Add(dropdown);
            }

            foreach (string name in RayTracingToggleDropdownNames)
            {
                DropdownField dropdown = RequireElement<DropdownField>(name);
                SetDropdownChoices(dropdown, Toggles.Keys, SettingsDropdownTitles.GetToggleTitle);
                rayTracingEffectDropdowns.Add(dropdown);
            }

            pathTracingDropdown = RequireElement<DropdownField>(PathTracingDropdownName);
            SetDropdownChoices(pathTracingDropdown, Toggles.Keys, SettingsDropdownTitles.GetToggleTitle);
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

            bool rayTracing = SystemInfo.supportsRayTracing;
            pathTracingDropdown.SetEnabled(rayTracing);
            foreach (DropdownField dropdown in rayTracingEffectDropdowns)
            {
                dropdown.SetEnabled(rayTracing && clientConfig.PathTracing != Toggles.On);
            }
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
