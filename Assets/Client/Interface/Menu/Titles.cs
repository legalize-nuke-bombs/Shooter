using System.Collections.Generic;

namespace Shooter.Client.Interface
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
            [Configuring.Antialiasings.Off] = "Выкл",
            [Configuring.Antialiasings.Fxaa] = "FXAA",
            [Configuring.Antialiasings.Taa] = "TAA",
            [Configuring.Antialiasings.Smaa] = "SMAA"
        };

        private static readonly Dictionary<string, string> Upscalers = new()
        {
            [Configuring.Upscalers.Off] = "Выкл",
            [Configuring.Upscalers.Dlaa] = "DLAA",
            [Configuring.Upscalers.Quality] = "Качество",
            [Configuring.Upscalers.Balanced] = "Баланс",
            [Configuring.Upscalers.Performance] = "Производительность"
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
