using System;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Bootstrapping
{
    public static class Bootstrap
    {
        private static readonly Journal Log = Logs.Here();

        private const string NameArgument = "-name";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            string instance = Instance();

            Logs.ToFile("shooter-" + instance);
            Logs.Least = Config.Read().Logging.Level;
            Log.Info($"Bootstrapping the {instance}, logging from {Logs.Least} and up");

            // Netcode fills its serializer tables from a generated method in the same startup phase,
            // and the order between assemblies is not defined. The session starts from Start, a frame
            // later, when that work is certainly done.
            var session = new GameObject(nameof(Session));
            session.AddComponent<Session>();
            UnityEngine.Object.DontDestroyOnLoad(session);
        }

        // Virtual players of the multiplayer play mode are handed their name on the command line, and
        // everything else is either the editor itself or a standalone build.
        private static string Instance()
        {
            string[] arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == NameArgument) return arguments[i + 1].ToLowerInvariant();
            }

            return Application.isEditor ? "editor" : "game";
        }
    }
}
