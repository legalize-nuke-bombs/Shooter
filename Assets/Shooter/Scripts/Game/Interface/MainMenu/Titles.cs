using System.Collections.Generic;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public static class Titles
    {
        private static readonly Dictionary<string, string> Compressions = new()
        {
            [""] = "Выкл",
            ["Zip"] = "Zip"
        };

        private static readonly Dictionary<string, string> Providers = new()
        {
            [""] = "Выкл"
        };

        private static readonly Dictionary<string, string> Antialiasings = new()
        {
            [Infrastructure.Antialiasings.Off] = "Выкл",
            [Infrastructure.Antialiasings.Fxaa] = "FXAA",
            [Infrastructure.Antialiasings.Taa] = "TAA",
            [Infrastructure.Antialiasings.Smaa] = "SMAA"
        };

        private static readonly Dictionary<string, string> Upscalers = new()
        {
            [Infrastructure.Upscalers.Off] = "Выкл",
            [Infrastructure.Upscalers.Dlaa] = "Максимум",
            [Infrastructure.Upscalers.Quality] = "Качество",
            [Infrastructure.Upscalers.Balanced] = "Баланс",
            [Infrastructure.Upscalers.Performance] = "Производительность"
        };

        public static string Antialiasing(string key)
        {
            return Titled(Antialiasings, key);
        }

        public static string Upscaler(string key)
        {
            return Titled(Upscalers, key);
        }

        public static string Compression(string key)
        {
            return Titled(Compressions, key);
        }

        public static string Provider(string key)
        {
            return Titled(Providers, key);
        }

        private static string Titled(Dictionary<string, string> titles, string key)
        {
            return titles.TryGetValue(key ?? "", out string title) ? title : key;
        }
    }
}
