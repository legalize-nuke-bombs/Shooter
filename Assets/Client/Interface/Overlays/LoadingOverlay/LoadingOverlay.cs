using Shooter.Client.Playing;
using Shooter.Game;
using Shooter.Logging;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    public class LoadingOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string ScreenElement = "loading";
        private const string TextElement = "loading-text";

        private const string Starting = "Инициализация";
        private const string Connecting = "Соединение — ";
        private const string Nameless = "сервер не указан";
        private const string Waiting = "Синхронизация мира";
        private const string Entering = "Вход в мир";
        private const string Lost = "Связь потеряна";

        private VisualElement screen;
        private Label text;
        private string shown;
        private bool entered;

        private void Update()
        {
            if (!Bound) return;

            string stage = Stage();
            if (stage == shown) return;

            shown = stage;
            Show(stage);

            if (stage != null) Log.Info("Loading screen says: {}", stage);
        }

        protected override bool Bind(VisualElement root)
        {
            screen = root.Q<VisualElement>(ScreenElement);
            text = root.Q<Label>(TextElement);
            entered = false;

            if (screen == null || text == null)
            {
                Log.Error("Overlay document has no {} screen, the loading state stays hidden", ScreenElement);
                return false;
            }

            shown = Stage();
            Show(shown);

            return true;
        }

        protected override void Unbind()
        {
            screen = null;
            text = null;
        }

        private void Show(string stage)
        {
            screen.style.display = stage == null ? DisplayStyle.None : DisplayStyle.Flex;

            if (stage != null) text.text = stage;
        }

        private string Stage()
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsListening) return Starting;

            if (!network.IsConnectedClient) return entered ? Lost : Connecting + Address(network);
            if (Environment.Current == null) return Waiting;
            if (OwnPlayer.Find<Transform>() == null) return Entering;

            entered = true;

            return null;
        }

        private static string Address(NetworkManager network)
        {
            var transport = network.GetComponent<UnityTransport>();
            if (transport == null) return Nameless;

            return transport.ConnectionData.Address + ":" + transport.ConnectionData.Port;
        }
    }
}
