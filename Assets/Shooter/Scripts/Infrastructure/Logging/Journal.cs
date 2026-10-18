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
            Say(Level.Debug, LogType.Log, message);
        }

        [HideInCallstack]
        public void Info(string message)
        {
            Say(Level.Info, LogType.Log, message);
        }

        [HideInCallstack]
        public void Warn(string message)
        {
            Say(Level.Warn, LogType.Warning, message);
        }

        [HideInCallstack]
        public void Error(string message)
        {
            Say(Level.Error, LogType.Error, message);
        }

        [HideInCallstack]
        private void Say(Level level, LogType type, string message)
        {
            if (level < Logs.Least) return;

            UnityEngine.Debug.unityLogger.Log(type, name + ": " + message);
        }
    }
}
