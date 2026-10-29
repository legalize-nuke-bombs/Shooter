using System.Collections.Generic;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public static class SettingsDropdownTitles
    {
        private static readonly Dictionary<string, string> CompressionTitles = new()
        {
            [""] = "Выкл",
            ["Zip"] = "Zip"
        };

        private static readonly Dictionary<string, string> ProviderTitles = new()
        {
            [""] = "Выкл"
        };

        private static readonly Dictionary<string, string> EffortTitles = new()
        {
            [LlmEfforts.Low] = "Низкая",
            [LlmEfforts.Medium] = "Средняя",
            [LlmEfforts.High] = "Высокая",
            [LlmEfforts.Max] = "Максимальная"
        };

        private static readonly Dictionary<string, string> UpscalerTitles = new()
        {
            [Upscalers.Off] = "Выкл",
            [Upscalers.Dlss] = "DLSS",
            [Upscalers.Stp] = "STP"
        };

        private static readonly Dictionary<string, string> AntialiasingTitles = new()
        {
            [Antialiasings.Off] = "Выкл",
            [Antialiasings.Fxaa] = "FXAA",
            [Antialiasings.Taa] = "TAA",
            [Antialiasings.Smaa] = "SMAA"
        };

        private static readonly Dictionary<string, string> FogTitles = new()
        {
            [FogQualities.Low] = "Низкий",
            [FogQualities.Medium] = "Средний",
            [FogQualities.High] = "Высокий",
            [FogQualities.Max] = "Максимальный"
        };

        public static string GetScreenResolutionTitle(string key)
        {
            return (key ?? "").Replace("x", " × ");
        }

        public static string GetUpscalerTitle(string key)
        {
            return GetTitleOrKey(UpscalerTitles, key);
        }

        public static string GetRenderScaleTitle(string key)
        {
            return $"{key}%";
        }

        public static string GetAntialiasingTitle(string key)
        {
            return GetTitleOrKey(AntialiasingTitles, key);
        }

        public static string GetFogTitle(string key)
        {
            return GetTitleOrKey(FogTitles, key);
        }

        public static string GetEffortTitle(string key)
        {
            return GetTitleOrKey(EffortTitles, key);
        }

        public static string GetCompressionTitle(string key)
        {
            return GetTitleOrKey(CompressionTitles, key);
        }

        public static string GetProviderTitle(string key)
        {
            return GetTitleOrKey(ProviderTitles, key);
        }

        private static string GetTitleOrKey(Dictionary<string, string> titlesByKey, string key)
        {
            return titlesByKey.TryGetValue(key ?? "", out string title) ? title : key;
        }
    }
}
