using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Game;
using Shooter.Logging;

namespace Shooter.Client.Interface.Overlays
{
    [RequireComponent(typeof(PanelRenderer))]
    public class VersionOverlay : MonoBehaviour
    {
        private const string ClientElement = "client-version";
        private const string ServerElement = "server-version";
        private const string Disconnected = "Сервер — не подключён";
        private const string NamelessWorld = "мир без имени";

        private PanelRenderer panel;
        private Label server;
        private bool connected;

        private void OnEnable()
        {
            panel = GetComponent<PanelRenderer>();
            panel.RegisterUIReloadCallback(Bind);
        }

        private void OnDisable()
        {
            panel.UnregisterUIReloadCallback(Bind);
            server = null;
        }

        private void Update()
        {
            if (server == null) return;

            Environment environment = Environment.Current;
            bool present = environment != null;

            if (present == connected) return;

            connected = present;
            server.text = present ? Describe(environment) : Disconnected;
        }

        private void Bind(PanelRenderer renderer, VisualElement root)
        {
            Label client = root.Q<Label>(ClientElement);
            server = root.Q<Label>(ServerElement);
            connected = false;

            if (client == null || server == null)
            {
                Log.Error("Overlay document has no {} or {} label, versions stay hidden", ClientElement, ServerElement);
                return;
            }

            client.text = "Клиент " + Application.version;
            server.text = Disconnected;
        }

        private static string Describe(Environment environment)
        {
            string world = string.IsNullOrEmpty(environment.World) ? NamelessWorld : environment.World;

            return "Сервер " + environment.Version + " — " + world;
        }
    }
}
