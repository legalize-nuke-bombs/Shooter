using UnityEngine;

namespace Shooter.Infrastructure
{
    public sealed class Journal
    {
        private readonly string name;

        internal Journal(string name)
        {
            this.name = name;
        }

        [HideInCallstack]
        public void Debug(string message)
        {
            Say(LogLevel.Debug, message);
        }

        [HideInCallstack]
        public void Info(string message)
        {
            Say(LogLevel.Info, message);
        }

        [HideInCallstack]
        public void Warn(string message)
        {
            Say(LogLevel.Warn, message);
        }

        [HideInCallstack]
        public void Error(string message)
        {
            Say(LogLevel.Error, message);
        }

        [HideInCallstack]
        private void Say(LogLevel level, string message)
        {
            if (level < Logs.Least) return;

            Logs.Write(level, name + ": " + message);
        }
    }
}
