using System;
using UnityEngine;

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

        public static void Info(string message)
        {
            Debug.Log(Line("INFO ", message));
        }

        public static void Warn(string message)
        {
            Debug.LogWarning(Line("WARN ", message));
        }

        public static void Error(string message)
        {
            Debug.LogError(Line("ERROR", message));
        }

        private static string Line(string level, string message)
        {
            return DateTime.Now.ToString("HH:mm:ss.fff") + " " + level + " [" + ThreadName() + "] " + message;
        }

        private static string ThreadName()
        {
            return System.Threading.Thread.CurrentThread.Name ?? "main";
        }
    }
}
