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
        private static readonly object Gate = new object();
        private static StreamWriter file;

        public static void ToFile(string path)
        {
            lock (Gate)
            {
                file?.Dispose();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                file = new StreamWriter(path, false) { AutoFlush = true };
            }
            Info("Log file opened at {}", path);
        }

        public static void Info(string template, params object[] args)
        {
            Emit(Line("INFO", Caller(), template, args));
        }

        public static void Warn(string template, params object[] args)
        {
            Emit(Line("WARN", Caller(), template, args));
        }

        public static void Error(string template, params object[] args)
        {
            Emit(Line("ERROR", Caller(), template, args));
        }

        private static void Emit(string line)
        {
            lock (Gate)
            {
                file?.WriteLine(line);
            }
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
