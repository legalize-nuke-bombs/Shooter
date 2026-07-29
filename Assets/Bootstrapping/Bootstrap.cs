using System;
using System.Diagnostics;
using System.IO;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Bootstrapping
{
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            Log.ToFile(InHome("shooter-" + Process.GetCurrentProcess().Id + ".log"));
            Log.Info("Bootstrapping process {}...", Process.GetCurrentProcess().Id);

            // Netcode fills its serializer tables from a generated method in the same startup phase,
            // and the order between assemblies is not defined. The session starts from Start, a frame
            // later, when that work is certainly done.
            var session = new GameObject(nameof(Session));
            session.AddComponent<Session>();
            UnityEngine.Object.DontDestroyOnLoad(session);
        }

        private static string InHome(string fileName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fileName);
        }
    }
}
