using System;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Bootstrapping
{
    public static class Bootstrap
    {
        private const string NameArgument = "-name";
        private static readonly Journal Log = Logs.Here();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            string instance = Instance();

            Logs.ToFile("shooter-" + instance);
            Logs.Least = Config.Read().Logging.Level;
            Log.Info($"Bootstrapping the {instance}, logging from {Logs.Least} and up");

            var session = new GameObject(nameof(Session));
            session.AddComponent<Session>();
            Object.DontDestroyOnLoad(session);
        }

        private static string Instance()
        {
            string[] arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length - 1; i++)
                if (arguments[i] == NameArgument)
                    return arguments[i + 1].ToLowerInvariant();

            return Application.isEditor ? "editor" : "game";
        }
    }
}
