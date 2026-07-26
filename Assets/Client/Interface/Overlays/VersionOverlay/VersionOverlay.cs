using Shooter.Game;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    public class VersionOverlay : Overlay
    {
        private const string VersionElement = "version";
        private const string Disconnected = "не подключён";
        private const string NamelessWorld = "мир без имени";

        private Label version;
        private bool connected;

        private void Update()
        {
            if (!Bound) return;

            Environment environment = Environment.Current;
            bool present = environment != null;

            if (present == connected) return;

            connected = present;
            version.text = Describe(environment);
        }

        protected override bool Bind(VisualElement root)
        {
            version = root.Q<Label>(VersionElement);
            connected = false;

            if (version == null)
            {
                Log.Error("Overlay document has no {} label, versions stay hidden", VersionElement);
                return false;
            }

            version.text = Describe(null);

            return true;
        }

        protected override void Unbind()
        {
            version = null;
        }

        private static string Describe(Environment environment)
        {
            string client = "Клиент " + Application.version;

            if (environment == null) return client + "   Сервер " + Disconnected;

            string world = string.IsNullOrEmpty(environment.World) ? NamelessWorld : environment.World;

            return client + "   Сервер " + environment.Version + "   " + world;
        }
    }
}
