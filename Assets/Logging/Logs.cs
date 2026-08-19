using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shooter.Logging
{
    public static class Logs
    {
        private const int SameNameLimit = 10;

        private static readonly object Gate = new();

        private static StreamWriter file;

        public static Level Least { get; set; } = Level.Info;

        public static Journal Here([CallerFilePath] string path = null)
        {
            return new Journal(string.IsNullOrEmpty(path) ? "?" : Path.GetFileNameWithoutExtension(path));
        }

        public static void ToFile(string name)
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
            return Application.isEditor
                ? Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Logs")
                : Application.persistentDataPath;
        }

        private static StreamWriter Opened(string folder, string name)
        {
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            for (int taken = 0; taken < SameNameLimit; taken++)
            {
                string path = Path.Combine(folder, taken == 0 ? name + ".log" : name + "-" + taken + ".log");

                try
                {
                    var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    var writer = new StreamWriter(stream) { AutoFlush = true };
                    writer.WriteLine(Stamped(LogType.Log, "Logs: writing to " + path));

                    return writer;
                }
                catch (IOException)
                {

                }
                catch (Exception e)
                {
                    Debug.unityLogger.Log(LogType.Warning, "Logs: file " + path + " can not be opened: " + e.Message);

                    return null;
                }
            }

            Debug.unityLogger.Log(LogType.Warning, "Logs: all " + SameNameLimit + " names of " + name +
                                                   " are busy in " + folder + ", this run stays in the console");

            return null;
        }

        private static void OnEngineLog(string message, string stackTrace, LogType type)
        {
            string line = Stamped(type, message);

            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                string trace = stackTrace?.TrimEnd();
                if (!string.IsNullOrEmpty(trace)) line += Environment.NewLine + trace;
            }

            lock (Gate)
            {
                file?.WriteLine(line);
            }
        }

        private static string Stamped(LogType type, string message)
        {
            return DateTime.Now.ToString("HH:mm:ss.fff") + " " + LevelOf(type) + " [" + ThreadName() + "] " + message;
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

        private static string ThreadName()
        {
            return Thread.CurrentThread.Name ?? "main";
        }
    }
}
