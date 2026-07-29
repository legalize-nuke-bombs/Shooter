using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shooter.Logging
{
    public sealed class Journal
    {
        private readonly string name;

        internal Journal(string name)
        {
            this.name = name;
        }

        [HideInCallstack]
        public void Info(string template, params object[] args)
        {
            Say(Level.Info, LogType.Log, template, args);
        }

        [HideInCallstack]
        public void Warn(string template, params object[] args)
        {
            Say(Level.Warn, LogType.Warning, template, args);
        }

        [HideInCallstack]
        public void Error(string template, params object[] args)
        {
            Say(Level.Error, LogType.Error, template, args);
        }

        [HideInCallstack]
        private void Say(Level level, LogType type, string template, object[] args)
        {
            if (level < Logs.Least) return;

            Debug.unityLogger.Log(type, name + ": " + Filled(template, args));
        }

        private static string Filled(string template, object[] args)
        {
            if (args == null || args.Length == 0) return template;

            var builder = new StringBuilder(template.Length);
            int argIndex = 0;
            for (int i = 0; i < template.Length; i++)
            {
                if (argIndex < args.Length && i + 1 < template.Length && template[i] == '{' && template[i + 1] == '}')
                {
                    builder.Append(args[argIndex++] ?? "null");
                    i++;
                }
                else
                {
                    builder.Append(template[i]);
                }
            }

            return builder.ToString();
        }
    }
}
