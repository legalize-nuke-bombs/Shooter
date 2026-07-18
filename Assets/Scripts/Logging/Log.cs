using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shooter.Logging
{
    public static class Log
    {
        static Log()
        {
            if (!Application.isBatchMode) return;
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Info(string template, params object[] args)
        {
            Debug.Log(Line("INFO ", Caller(), template, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warn(string template, params object[] args)
        {
            Debug.LogWarning(Line("WARN ", Caller(), template, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(string template, params object[] args)
        {
            Debug.LogError(Line("ERROR", Caller(), template, args));
        }

        private static string Line(string level, string caller, string template, object[] args)
        {
            return DateTime.Now.ToString("HH:mm:ss.fff") + " " + level + " [" + ThreadName() + "] " + caller + ": " + Format(template, args);
        }

        private static string Format(string template, object[] args)
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string Caller()
        {
            return new StackFrame(2, false).GetMethod()?.DeclaringType?.Name ?? "?";
        }

        private static string ThreadName()
        {
            return System.Threading.Thread.CurrentThread.Name ?? "main";
        }
    }
}
