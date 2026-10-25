using UnityEngine;

namespace Shooter.Infrastructure
{
    public sealed class Journal
    {
        private readonly string sourceFileName;

        internal Journal(string sourceFileName)
        {
            this.sourceFileName = sourceFileName;
        }

        [HideInCallstack]
        public void Debug(string message)
        {
            WriteAtLevel(LogLevel.Debug, message);
        }

        [HideInCallstack]
        public void Debug(ref DebugInterpolatedStringHandler message)
        {
            if (message.IsEnabled) WriteAtLevel(LogLevel.Debug, message.ToString());
        }

        [HideInCallstack]
        public void Info(string message)
        {
            WriteAtLevel(LogLevel.Info, message);
        }

        [HideInCallstack]
        public void Warn(string message)
        {
            WriteAtLevel(LogLevel.Warn, message);
        }

        [HideInCallstack]
        public void Error(string message)
        {
            WriteAtLevel(LogLevel.Error, message);
        }

        [HideInCallstack]
        private void WriteAtLevel(LogLevel level, string message)
        {
            if (level < Logs.MinimumLevel) return;

            Logs.WriteToUnityLogger(level, sourceFileName + ": " + message);
        }
    }
}
