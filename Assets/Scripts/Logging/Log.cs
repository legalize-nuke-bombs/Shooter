using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shooter.Logging
{
    public static class Log
    {
        private static readonly bool BatchMode;
        private static readonly object gate = new object();
        private static StreamWriter file;

        static Log()
        {
            BatchMode = Application.isBatchMode;
        }

        public static void ToFile(string path)
        {
            lock (gate)
            {
                file?.Dispose();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                file = new StreamWriter(path, false) { AutoFlush = true };
            }
            Info("Log file opened at {}", path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Info(string template, params object[] args)
        {
            Emit(LogType.Log, Line("INFO", Caller(), template, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warn(string template, params object[] args)
        {
            Emit(LogType.Warning, Line("WARN", Caller(), template, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(string template, params object[] args)
        {
            Emit(LogType.Error, Line("ERROR", Caller(), template, args));
        }

        private static void Emit(LogType type, string line)
        {
            switch (type)
            {
                case LogType.Warning:
                    Debug.LogWarning(line);
                    break;
                case LogType.Error:
                    Debug.LogError(line);
                    break;
                default:
                    Debug.Log(line);
                    break;
            }

            lock (gate)
            {
                file?.WriteLine(line);
            }
        }

        private static string Line(string level, string caller, string template, object[] args)
        {
            return DateTime.Now.ToString("HH:mm:ss.fff") + " " + level + " [" + (BatchMode ? "Server" : "Client") + "] [" + ThreadName() + "] " + caller + ": " + Format(template, args);
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
