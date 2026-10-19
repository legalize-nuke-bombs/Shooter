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

        private static readonly Dictionary<string, string> AntialiasingTitles = new()
        {
            [Antialiasings.Off] = "Выкл",
            [Antialiasings.Fxaa] = "FXAA",
            [Antialiasings.Taa] = "TAA",
            [Antialiasings.Smaa] = "SMAA"
        };

        private static readonly Dictionary<string, string> UpscalerTitles = new()
        {
            [Upscalers.Off] = "Выкл",
            [Upscalers.Dlaa] = "Максимум",
            [Upscalers.Quality] = "Качество",
            [Upscalers.Balanced] = "Баланс",
            [Upscalers.Performance] = "Производительность"
        };

        public static string GetAntialiasingTitle(string key)
        {
            return GetTitleOrKey(AntialiasingTitles, key);
        }

        public static string GetUpscalerTitle(string key)
        {
            return GetTitleOrKey(UpscalerTitles, key);
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
