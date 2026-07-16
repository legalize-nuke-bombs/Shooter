using System;
using UnityEngine;

public static class ServerLog
{
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
        return DateTime.Now.ToString("HH:mm:ss.fff") + " " + level + " [" + Thread() + "] " + message;
    }

    private static string Thread()
    {
        return System.Threading.Thread.CurrentThread.Name ?? "main";
    }
}
