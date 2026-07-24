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
        private const string EngineCaller = "Unity";

        private static readonly object Gate = new object();
        private static StreamWriter file;

        [ThreadStatic]
        private static bool mirroring;

        public static void ToFile(string path)
        {
            lock (Gate)
            {
                file?.Dispose();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                file = new StreamWriter(path, false) { AutoFlush = true };
            }

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.logMessageReceivedThreaded -= OnEngineLog;
            Application.logMessageReceivedThreaded += OnEngineLog;

            Info("Log file opened at {}", path);
        }

        public static void Info(string template, params object[] args)
        {
            Emit(LogType.Log, Line("INFO", Caller(), template, args));
        }

        public static void Warn(string template, params object[] args)
        {
            Emit(LogType.Warning, Line("WARN", Caller(), template, args));
        }

        public static void Error(string template, params object[] args)
        {
            Emit(LogType.Error, Line("ERROR", Caller(), template, args));
        }

        private static void Emit(LogType type, string line)
        {
            Write(line);

            mirroring = true;
            try
            {
                Debug.unityLogger.Log(type, line);
            }
            finally
            {
                mirroring = false;
            }
        }

        private static void OnEngineLog(string message, string stackTrace, LogType type)
        {
            if (mirroring) return;

            string line = Line(LevelOf(type), EngineCaller, message, null);
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                string trace = stackTrace?.TrimEnd();
                if (!string.IsNullOrEmpty(trace)) line += Environment.NewLine + trace;
            }

            Write(line);
        }

        private static string LevelOf(LogType type)
        {
            return type switch
            {
                LogType.Warning => "WARN",
                LogType.Error or LogType.Exception or LogType.Assert => "ERROR",
                _ => "INFO"
            };
        }

        private static void Write(string line)
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
