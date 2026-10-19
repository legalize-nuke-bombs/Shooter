using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shooter.Infrastructure
{
    public static class Logs
    {
        private const int SameNameLimit = 10;
        private const string NameArgument = "-name";

        private static readonly object Gate = new();

        private static StreamWriter file;
        private static int mainThread;

        [ThreadStatic] private static LogLevel? writingLevel;

        public static LogLevel Least { get; private set; } = LogLevel.Info;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Begin()
        {
            mainThread = Thread.CurrentThread.ManagedThreadId;
            string instance = Instance();
            ToFile("shooter-" + instance);
            Least = Config.Read().Logging.Level;
            Here().Info($"Logging the {instance} from {Least} and up");
        }

        private static string Instance()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == NameArgument) return arguments[i + 1].ToLowerInvariant();
            }

            return Application.isEditor ? "editor" : "game";
        }

        public static Journal Here([CallerFilePath] string path = null)
        {
            return new Journal(string.IsNullOrEmpty(path) ? "?" : Path.GetFileNameWithoutExtension(path));
        }

        [HideInCallstack]
        internal static void Write(LogLevel level, string line)
        {
            LogType type = level switch
            {
                LogLevel.Warn => LogType.Warning,
                LogLevel.Error => LogType.Error,
                _ => LogType.Log
            };

            writingLevel = level;
            try
            {
                Debug.unityLogger.Log(type, line);
            }
            finally
            {
                writingLevel = null;
            }
        }

        private static void ToFile(string name)
        {
            lock (Gate)
            {
                file?.Dispose();
                file = Opened(Folder(), name);
            }

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            Application.logMessageReceivedThreaded -= OnEngineLog;
            Application.logMessageReceivedThreaded += OnEngineLog;
        }

        private static string Folder()
        {
            return Application.isEditor ? Path.Combine(Config.Root(), "Logs") : Config.Root();
        }

        private static StreamWriter Opened(string folder, string name)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            for (int taken = 0; taken < SameNameLimit; taken++)
            {
                string path = Path.Combine(folder, taken == 0 ? name + ".log" : name + "-" + taken + ".log");

                try
                {
                    FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    StreamWriter writer = new(stream) { AutoFlush = true };
                    writer.WriteLine(Stamped(LogType.Log, "Logs: writing to " + path));

                    return writer;
                }
                catch (IOException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    Debug.unityLogger.Log(LogType.Warning, "Logs: file " + path + " can not be opened: " + e.Message);

                    return null;
                }
            }

            Debug.unityLogger.Log(LogType.Warning, $"Logs: all {SameNameLimit} names of {name} are busy in {folder}, this run stays in the console");

            return null;
        }

        private static void OnEngineLog(string message, string stackTrace, LogType type)
        {
            string line = Stamped(type, message);

            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                string trace = stackTrace?.TrimEnd();
                if (!string.IsNullOrEmpty(trace))
                {
                    line += Environment.NewLine + trace;
                }
            }

            lock (Gate)
            {
                file?.WriteLine(line);
            }
        }

        private static string Stamped(LogType type, string message)
        {
            return DateTime.Now.ToString("HH:mm:ss.fff") + " " + Label(type) + " [" + ThreadName() + "] " + message;
        }

        private static string Label(LogType type)
        {
            if (writingLevel == LogLevel.Debug) return "DEBUG";

            return type switch
            {
                LogType.Warning => "WARN",
                LogType.Error or LogType.Exception or LogType.Assert => "ERROR",
                _ => "INFO"
            };
        }

        private static string ThreadName()
        {
            Thread current = Thread.CurrentThread;
            if (current.ManagedThreadId == mainThread) return "main";

            return current.Name ?? "thread " + current.ManagedThreadId;
        }
    }
}
