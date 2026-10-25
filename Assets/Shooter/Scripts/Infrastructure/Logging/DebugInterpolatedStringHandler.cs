using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Shooter.Infrastructure
{
    // The compiler asks the constructor first and, when it says no, skips the holes of the string: nothing is evaluated or built while Debug is off
    [InterpolatedStringHandler]
    public ref struct DebugInterpolatedStringHandler
    {
        private readonly StringBuilder builder;

        public DebugInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled)
        {
            isEnabled = Logs.MinimumLevel <= LogLevel.Debug;
            builder = isEnabled ? new StringBuilder(literalLength + formattedCount * 16) : null;
        }

        public bool IsEnabled => builder != null;

        public void AppendLiteral(string literal)
        {
            builder.Append(literal);
        }

        public void AppendFormatted<T>(T value)
        {
            builder.Append(value);
        }

        public void AppendFormatted<T>(T value, string format) where T : IFormattable
        {
            builder.Append(value.ToString(format, null));
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}
