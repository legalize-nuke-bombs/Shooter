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

        private static readonly object FileLock = new();

        private static StreamWriter fileWriter;
        private static int mainThreadId;

        [ThreadStatic] private static LogLevel? writingLevel;

        public static LogLevel MinimumLevel { get; private set; } = LogLevel.Info;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void StartLogging()
        {
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            string instanceName = GetInstanceName();
            StartWritingToFile("shooter-" + instanceName);
            MinimumLevel = Config.GetGameConfig().Logging.Level;
            Here().Info($"Logging the {instanceName} from {MinimumLevel} and up");
        }

        private static string GetInstanceName()
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
        internal static void WriteToUnityLogger(LogLevel level, string line)
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

        private static void StartWritingToFile(string fileName)
        {
            lock (FileLock)
            {
                fileWriter?.Dispose();
                fileWriter = OpenFirstFreeLogFile(GetLogFolder(), fileName);
            }

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            Application.logMessageReceivedThreaded -= WriteLogMessageToFile;
            Application.logMessageReceivedThreaded += WriteLogMessageToFile;
        }

        private static string GetLogFolder()
        {
            return Application.isEditor ? Path.Combine(Config.GetRootFolder(), "Logs") : Config.GetRootFolder();
        }

        private static StreamWriter OpenFirstFreeLogFile(string folder, string fileName)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            for (int suffix = 0; suffix < SameNameLimit; suffix++)
            {
                string path = Path.Combine(folder, suffix == 0 ? fileName + ".log" : fileName + "-" + suffix + ".log");

                try
                {
                    FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    StreamWriter writer = new(stream) { AutoFlush = true };
                    writer.WriteLine(FormatLogLine(LogType.Log, "Logs: writing to " + path));

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

            Debug.unityLogger.Log(LogType.Warning, $"Logs: all {SameNameLimit} names of {fileName} are busy in {folder}, this run stays in the console");

            return null;
        }

        private static void WriteLogMessageToFile(string message, string stackTrace, LogType type)
        {
            string line = FormatLogLine(type, message);

            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                string trace = stackTrace?.TrimEnd();
                if (!string.IsNullOrEmpty(trace))
                {
                    line += Environment.NewLine + trace;
                }
            }

            lock (FileLock)
            {
                fileWriter?.WriteLine(line);
            }
        }

        private static string FormatLogLine(LogType type, string message)
        {
            return DateTime.Now.ToString("HH:mm:ss.fff") + " " + GetLevelLabel(type) + " [" + GetThreadName() + "] " + message;
        }

        private static string GetLevelLabel(LogType type)
        {
            if (writingLevel == LogLevel.Debug) return "DEBUG";

            return type switch
            {
                LogType.Warning => "WARN",
                LogType.Error or LogType.Exception or LogType.Assert => "ERROR",
                _ => "INFO"
            };
        }

        private static string GetThreadName()
        {
            Thread currentThread = Thread.CurrentThread;
            if (currentThread.ManagedThreadId == mainThreadId) return "main";

            return currentThread.Name ?? "thread " + currentThread.ManagedThreadId;
        }
    }
}
