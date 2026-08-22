using System.Collections.Generic;

namespace Shooter.Client.Interface
{
    public static class Titles
    {
        private static readonly Dictionary<string, string> Compressions = new()
        {
            [""] = "Папка",
            ["Zip"] = "Zip-архив"
        };

        private static readonly Dictionary<string, string> Providers = new()
        {
            [""] = "Нет"
        };

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
