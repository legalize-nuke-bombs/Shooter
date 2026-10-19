using System.Globalization;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    internal static class PortConverters
    {
        private const ushort FallbackPort = 7777;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();
        private static bool registered;

        public static void RegisterGlobalConverters()
        {
            if (registered) return;

            registered = true;
            ConverterGroups.RegisterGlobalConverter((ref ushort port) => port.ToString(CultureInfo.InvariantCulture));
            ConverterGroups.RegisterGlobalConverter((ref string typedPort) => ParsePortOrFallback(typedPort));
        }

        private static ushort ParsePortOrFallback(string typedPort)
        {
            if (ushort.TryParse(typedPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort port) && port != 0) return port;

            Log.Warn($"Port {typedPort} is not a number between 1 and 65535, using {FallbackPort}");

            return FallbackPort;
        }
    }
}
