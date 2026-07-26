using Shooter.Game;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    public class VersionOverlay : Overlay
    {
        private const string ClientElement = "client-version";
        private const string ServerElement = "server-version";
        private const string Disconnected = "Сервер — не подключён";
        private const string NamelessWorld = "мир без имени";

        private Label server;
        private bool connected;

        private void Update()
        {
            if (!Bound) return;

            Environment environment = Environment.Current;
            bool present = environment != null;

            if (present == connected) return;

            connected = present;
            server.text = present ? Describe(environment) : Disconnected;
        }

        protected override bool Bind(VisualElement root)
        {
            Label client = root.Q<Label>(ClientElement);
            server = root.Q<Label>(ServerElement);
            connected = false;

            if (client == null || server == null)
            {
                Log.Error("Overlay document has no {} or {} label, versions stay hidden", ClientElement, ServerElement);
                return false;
            }

            client.text = "Клиент " + Application.version;
            server.text = Disconnected;

            return true;
        }

        protected override void Unbind()
        {
            server = null;
        }

        private static string Describe(Environment environment)
        {
            string world = string.IsNullOrEmpty(environment.World) ? NamelessWorld : environment.World;

            return "Сервер " + environment.Version + " — " + world;
        }
    }
}
