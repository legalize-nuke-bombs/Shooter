using System.Globalization;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    internal static class PortConverters
    {
        private const ushort FallbackPort = 7777;
        private static readonly Journal Log = Logs.Here();
        private static bool registered;

        public static void Register()
        {
            if (registered) return;

            registered = true;
            ConverterGroups.RegisterGlobalConverter((ref ushort port) => port.ToString(CultureInfo.InvariantCulture));
            ConverterGroups.RegisterGlobalConverter((ref string typed) => Parse(typed));
        }

        private static ushort Parse(string typed)
        {
            if (ushort.TryParse(typed, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort port) && port != 0)
                return port;

            Log.Warn($"Port {typed} is not a number between 1 and 65535, using {FallbackPort}");

            return FallbackPort;
        }
    }
}
